const REQUIRED_PROPERTIES = ["TELEGRAM_BOT_TOKEN", "TELEGRAM_CHAT_ID", "SPREADSHEET_ID"];

function doPost(e) {
  try {
    const properties = PropertiesService.getScriptProperties();
    const config = getConfig(properties);
    const order = readOrder(e);
    validateOrder(order);

    const now = new Date();
    const lock = LockService.getScriptLock();
    lock.waitLock(10000);
    let orderId;
    try {
      orderId = createOrderId(now, config.sheet);
      appendOrder(config.sheet, now, orderId, order);
    } finally {
      lock.releaseLock();
    }
    sendTelegram(config.token, config.chatId, now, orderId, order);

    return jsonResponse({
      success: true,
      message: "Order submitted successfully",
      orderId: orderId
    });
  } catch (error) {
    console.error(error);
    return jsonResponse({
      success: false,
      message: "Unable to submit the order"
    });
  }
}

function getConfig(properties) {
  const values = properties.getProperties();
  const missing = REQUIRED_PROPERTIES.filter(function (key) { return !values[key]; });
  if (missing.length) throw new Error("Missing script properties");
  return {
    token: values.TELEGRAM_BOT_TOKEN,
    chatId: values.TELEGRAM_CHAT_ID,
    sheet: SpreadsheetApp.openById(values.SPREADSHEET_ID).getSheets()[0]
  };
}

function readOrder(e) {
  const params = (e && e.parameter) || {};
  return {
    name: clean(params.Name),
    phone: clean(params.Phone),
    serviceType: clean(params.ServiceType),
    neighborhood: clean(params.Neighborhood),
    message: clean(params.Message)
  };
}

function validateOrder(order) {
  if (!order.name || !order.phone) throw new Error("Required field missing");
  if (!/^0?5\d{8}$|^\+?9665\d{8}$/.test(order.phone)) throw new Error("Invalid phone");
}

function clean(value) {
  return String(value || "").trim().slice(0, 2000);
}

function createOrderId(now, sheet) {
  const datePart = Utilities.formatDate(now, Session.getScriptTimeZone(), "yyyyMMdd");
  const prefix = "ORD-" + datePart + "-";
  const lastRow = sheet.getLastRow();
  let sequence = 1;
  if (lastRow > 1) {
    const ids = sheet.getRange(2, 2, lastRow - 1, 1).getValues().flat();
    sequence = ids.filter(function (id) { return String(id).indexOf(prefix) === 0; }).length + 1;
  }
  return prefix + String(sequence).padStart(4, "0");
}

function appendOrder(sheet, now, orderId, order) {
  ensureHeaders(sheet);
  sheet.appendRow([
    now,
    orderId,
    Utilities.formatDate(now, Session.getScriptTimeZone(), "yyyy-MM-dd"),
    Utilities.formatDate(now, Session.getScriptTimeZone(), "HH:mm:ss"),
    order.name,
    order.phone,
    order.serviceType,
    order.neighborhood,
    order.message,
    "New"
  ]);
}

function ensureHeaders(sheet) {
  const headers = ["Timestamp", "Order ID", "Date", "Time", "Customer Name", "Phone", "Service", "Neighborhood", "Notes", "Status"];
  if (sheet.getLastRow() === 0) sheet.appendRow(headers);
}

function sendTelegram(token, chatId, now, orderId, order) {
  const text = [
    "🚚 طلب جديد من موقع شركة السلطان لنقل الأثاث",
    "🆔 رقم الطلب: " + orderId,
    "👤 الاسم: " + order.name,
    "📱 الجوال: " + order.phone,
    "🛠️ الخدمة: " + (order.serviceType || "غير محدد"),
    "📍 الحي: " + (order.neighborhood || "غير محدد"),
    "📝 التفاصيل: " + (order.message || "لا توجد"),
    "🕐 وقت الطلب: " + Utilities.formatDate(now, Session.getScriptTimeZone(), "yyyy/MM/dd HH:mm")
  ].join("\n");
  const response = UrlFetchApp.fetch("https://api.telegram.org/bot" + token + "/sendMessage", {
    method: "post",
    contentType: "application/json",
    payload: JSON.stringify({ chat_id: chatId, text: text }),
    muteHttpExceptions: true
  });
  if (response.getResponseCode() < 200 || response.getResponseCode() >= 300) throw new Error("Telegram request failed");
}

function jsonResponse(payload) {
  return ContentService.createTextOutput(JSON.stringify(payload)).setMimeType(ContentService.MimeType.JSON);
}