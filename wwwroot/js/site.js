// Key the thank-you page reads the just-submitted order's follow-up links from.
var ORDER_KEY = "alsultaan:lastOrder";

document.addEventListener("DOMContentLoaded", function () {
	document.querySelectorAll("[data-order-form]").forEach(function (form) {
		form.addEventListener("submit", submitOrder);
	});
});

async function submitOrder(event) {
	event.preventDefault();

	var form = event.currentTarget;
	var submitButton = form.querySelector("[data-order-submit]");
	var status = form.querySelector("[data-order-status]");
	var endpoint = form.dataset.appsScriptUrl;

	if (!form.checkValidity()) {
		form.classList.add("was-validated");
		form.reportValidity();
		return;
	}

	if (!endpoint) {
		showOrderStatus(status, "حدث خطأ أثناء إرسال الطلب، برجاء المحاولة مرة أخرى.", false);
		return;
	}

	submitButton.disabled = true;
	showOrderStatus(status, "جارٍ إرسال الطلب...", true);

	var submitted = false;
	try {
		var response = await fetchWithTimeout(endpoint, {
			method: "POST",
			mode: "cors",
			body: new URLSearchParams(new FormData(form))
		}, 15000);
		var result = await response.json();

		if (!response.ok || !result.success) {
			// Apps Script answers 200 with {success:false} when its own work fails
			// (missing Script Properties, Telegram or Sheets errors) — keep the reason.
			throw new Error((result && result.message) || "submission-failed");
		}

		submitted = true;
	} catch (error) {
		console.error("تعذر إرسال الطلب:", error);
		showOrderStatus(status, "حدث خطأ أثناء إرسال الطلب، برجاء المحاولة مرة أخرى.", false);
	} finally {
		submitButton.disabled = false;
	}

	// The WhatsApp/email follow-up now lives on the thank-you page so the form
	// stays compact. Handing over must never fail an order the server accepted,
	// hence the separate try and the redirect running either way.
	if (submitted) {
		showOrderStatus(status, "تم إرسال طلبك بنجاح، جارٍ تحويلك...", true);
		try {
			sessionStorage.setItem(ORDER_KEY, JSON.stringify(buildFollowUpUrls(form)));
		} catch (error) {
			console.error("تعذر حفظ تفاصيل الطلب:", error);
		}
		form.reset();
		window.location.assign(form.dataset.thankYouUrl || "/Contact/ThankYou");
	}
}

function fetchWithTimeout(url, options, timeout) {
	return Promise.race([
		fetch(url, options),
		new Promise(function (_, reject) {
			setTimeout(function () { reject(new Error("timeout")); }, timeout);
		})
	]);
}

function showOrderStatus(element, message, success) {
	element.textContent = message;
	element.className = "order-status alert " + (success ? "alert-success" : "alert-danger");
}

// Composes the wa.me and mailto links the thank-you page offers the customer.
function buildFollowUpUrls(form) {
	var values = new FormData(form);
	var text = [
		"🚚 طلب جديد من موقع شركة السلطان لنقل الأثاث",
		"——————————————",
		"👤 الاسم: " + (values.get("Name") || ""),
		"📱 الجوال: " + (values.get("Phone") || ""),
		"🛠️ الخدمة: " + (values.get("ServiceType") || ""),
		"📍 الحي: " + (values.get("Neighborhood") || ""),
		"📝 التفاصيل: " + (values.get("Message") || ""),
		"🕐 التاريخ: " + new Date().toLocaleString("ar-SA")
	].join("\n");
	var encodedText = encodeURIComponent(text);

	return {
		whatsapp: "https://wa.me/" + encodeURIComponent(form.dataset.whatsappNumber || "") + "?text=" + encodedText,
		email: "mailto:" + encodeURIComponent(form.dataset.businessEmail || "") +
			"?subject=" + encodeURIComponent("طلب جديد من موقع شركة السلطان") + "&body=" + encodedText
	};
}
