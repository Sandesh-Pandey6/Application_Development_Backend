"""Generate Postman collection and environment for AutoPartsPro API."""
import json
from pathlib import Path

OUT = Path(__file__).parent

def req(name, method, path, *, auth=True, body=None, query=None, formdata=False, desc=""):
    r = {
        "name": name,
        "request": {
            "method": method,
            "header": [],
            "url": {
                "raw": "{{baseUrl}}" + path + (("?" + "&".join(f"{k}={v}" for k, v in query.items())) if query else ""),
                "host": ["{{baseUrl}}"],
                "path": [p for p in path.strip("/").split("/") if p],
            },
            "description": desc,
        },
    }
    if query:
        r["request"]["url"] = {
            "raw": "{{baseUrl}}" + path + "?" + "&".join(f"{k}={v}" for k, v in query.items()),
            "host": ["{{baseUrl}}"],
            "path": [p for p in path.strip("/").split("/") if p],
            "query": [{"key": k, "value": v} for k, v in query.items()],
        }
    if not auth:
        r["request"]["auth"] = {"type": "noauth"}
    if body is not None:
        if formdata:
            r["request"]["body"] = {
                "mode": "formdata",
                "formdata": [{"key": "file", "type": "file", "src": []}],
            }
        else:
            r["request"]["header"].append({"key": "Content-Type", "value": "application/json"})
            r["request"]["body"] = {"mode": "raw", "raw": json.dumps(body, indent=2)}
    return r

def folder(name, items, desc=""):
    return {"name": name, "description": desc, "item": items}

LOGIN_TEST = {
    "listen": "test",
    "script": {
        "exec": [
            "if (pm.response.code === 200) {",
            "  const j = pm.response.json();",
            "  if (j.token) {",
            "    pm.collectionVariables.set('accessToken', j.token);",
            "    pm.environment.set('accessToken', j.token);",
            "  }",
            "}",
        ],
        "type": "text/javascript",
    },
}

def login_req(name, role):
    email = "{{adminEmail}}" if role == "Admin" else ("{{staffEmail}}" if role == "Staff" else "{{customerEmail}}")
    pwd = "{{adminPassword}}" if role == "Admin" else ("{{staffPassword}}" if role == "Staff" else "{{customerPassword}}")
    r = req(
        name,
        "POST",
        "/api/auth/login",
        auth=False,
        body={"email": email, "password": pwd, "role": role},
        desc=f"Login as {role}. Saves JWT to accessToken.",
    )
    r["event"] = [LOGIN_TEST]
    return r

collection = {
    "info": {
        "_postman_id": "autopartspro-api-collection",
        "name": "AutoPartsPro API",
        "description": "Complete API collection for AutoPartsPro (ASP.NET Core).\\n\\n**Setup**\\n1. Import `AutoPartsPro-Local.postman_environment.json`\\n2. Run API: `dotnet run` in Autopartspro project (http://localhost:5009)\\n3. Run **Auth > Login (Admin)** to set `accessToken`\\n\\n**Default admin:** admin@autopartspro.com / Admin@123",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
    },
    "auth": {
        "type": "bearer",
        "bearer": [{"key": "token", "value": "{{accessToken}}", "type": "string"}],
    },
    "variable": [
        {"key": "baseUrl", "value": "http://localhost:5009"},
        {"key": "accessToken", "value": ""},
        {"key": "adminEmail", "value": "admin@autopartspro.com"},
        {"key": "adminPassword", "value": "Admin@123"},
        {"key": "staffEmail", "value": ""},
        {"key": "staffPassword", "value": ""},
        {"key": "customerEmail", "value": ""},
        {"key": "customerPassword", "value": ""},
        {"key": "customerId", "value": ""},
        {"key": "partId", "value": ""},
        {"key": "vendorId", "value": ""},
        {"key": "staffId", "value": ""},
        {"key": "partRequestId", "value": ""},
        {"key": "invoiceId", "value": ""},
        {"key": "appointmentId", "value": ""},
        {"key": "purchaseInvoiceId", "value": ""},
        {"key": "notificationId", "value": ""},
        {"key": "vehicleId", "value": ""},
    ],
    "item": [
        folder("Health", [
            req("Health Check", "GET", "/api/health", auth=False, desc="No auth. Returns API and DB status."),
        ]),
        folder("Auth", [
            req("Register (Customer)", "POST", "/api/auth/register", auth=False, body={
                "role": "Customer", "fullName": "Test Customer", "email": "customer.test@example.com",
                "phoneNumber": "9800000001", "city": "Kathmandu", "password": "Test@123",
                "confirmPassword": "Test@123", "agreeToTerms": True, "make": "Honda", "model": "City",
                "year": 2020, "fuelType": "Petrol", "numberPlate": "BA 1 KA 1234",
            }),
            req("Register (Staff)", "POST", "/api/auth/register", auth=False, body={
                "role": "Staff", "fullName": "Test Staff", "email": "staff.test@example.com",
                "phoneNumber": "9800000002", "city": "Kathmandu", "password": "Test@123",
                "confirmPassword": "Test@123", "agreeToTerms": True, "employeeId": "EMP-001",
                "department": "Sales", "accessLevel": "Staff", "branchLocation": "Kathmandu - Main Branch",
            }),
            req("Verify Email", "POST", "/api/auth/verify-email", auth=False, body={
                "email": "customer.test@example.com", "otpCode": "123456", "purpose": "EmailVerification",
            }),
            login_req("Login (Admin)", "Admin"),
            login_req("Login (Staff)", "Staff"),
            login_req("Login (Customer)", "Customer"),
            req("Verify Login OTP", "POST", "/api/auth/verify-login", auth=False, body={
                "email": "admin@autopartspro.com", "otpCode": "123456", "purpose": "Login",
            }),
            req("Resend OTP", "POST", "/api/auth/resend-otp", auth=False, body={
                "email": "customer.test@example.com", "purpose": "EmailVerification",
            }),
            req("Forgot Password", "POST", "/api/auth/forgot-password", auth=False, body={
                "email": "customer.test@example.com",
            }),
            req("Reset Password", "POST", "/api/auth/reset-password", auth=False, body={
                "email": "customer.test@example.com", "otpCode": "123456",
                "newPassword": "NewTest@123", "confirmNewPassword": "NewTest@123",
            }),
            req("Get Current User (me)", "GET", "/api/auth/me"),
        ], "Public auth endpoints except GET /me"),
        folder("Admin", [
            req("Dashboard", "GET", "/api/admin/dashboard"),
            req("List Staff", "GET", "/api/admin/staff", query={"search": ""}),
            req("Get Staff", "GET", "/api/admin/staff/{{staffId}}"),
            req("Create Staff", "POST", "/api/admin/staff", body={
                "fullName": "New Staff", "email": "newstaff@autopartspro.com", "phoneNumber": "9800000010",
                "city": "Kathmandu", "password": "Staff@123", "employeeId": "EMP-100",
                "department": "Inventory", "accessLevel": "Staff", "branchLocation": "Kathmandu",
            }),
            req("Update Staff", "PUT", "/api/admin/staff/{{staffId}}", body={
                "fullName": "Updated Staff", "phoneNumber": "9800000011", "city": "Lalitpur",
                "department": "Sales", "accessLevel": "Staff", "branchLocation": "Kathmandu",
                "isApprovedByAdmin": True,
            }),
            req("Toggle Staff Status", "PATCH", "/api/admin/staff/{{staffId}}/toggle-status"),
            req("Approve Staff", "PATCH", "/api/admin/staff/{{staffId}}/approve"),
            req("Reject Staff", "PATCH", "/api/admin/staff/{{staffId}}/reject"),
            req("List Parts (paginated)", "GET", "/api/admin/parts", query={
                "search": "", "category": "", "stockLevel": "", "page": "1", "pageSize": "8",
            }),
            req("Get Part", "GET", "/api/admin/parts/{{partId}}"),
            req("Create Part", "POST", "/api/admin/parts", body={
                "partName": "Oil Filter", "description": "Standard oil filter", "category": "Filters",
                "price": 850, "stockQuantity": 50, "vendorId": "{{vendorId}}",
            }),
            req("Update Part", "PUT", "/api/admin/parts/{{partId}}", body={
                "partName": "Oil Filter", "description": "Updated", "category": "Filters",
                "price": 900, "stockQuantity": 45, "vendorId": "{{vendorId}}",
            }),
            req("Delete Part", "DELETE", "/api/admin/parts/{{partId}}"),
            req("List Purchase Invoices", "GET", "/api/admin/purchase-invoices"),
            req("Get Purchase Invoice", "GET", "/api/admin/purchase-invoices/{{purchaseInvoiceId}}"),
            req("Create Purchase Invoice", "POST", "/api/admin/purchase-invoices", body={
                "vendorInvoiceNumber": "VND-2026-001", "vendorId": "{{vendorId}}",
                "purchaseDate": "2026-05-19T00:00:00Z",
                "items": [{"productName": "Brake Pads", "quantity": 10, "unitPrice": 2500}],
            }),
            req("Update Purchase Invoice Status", "PATCH", "/api/admin/purchase-invoices/{{purchaseInvoiceId}}/status", query={"status": "Completed"}),
            req("Financial Reports", "GET", "/api/admin/financial-reports", query={"period": "monthly", "date": ""}),
            req("Financial Reports PDF", "GET", "/api/admin/financial-reports/pdf", query={"period": "monthly"}),
            req("Unpaid Sales Invoices", "GET", "/api/admin/unpaid-sales-invoices", query={"filter": "all"}),
            req("Mark Sales Invoice Paid", "PATCH", "/api/admin/unpaid-sales-invoices/{{invoiceId}}/mark-paid"),
            req("Sales Invoice PDF", "GET", "/api/admin/sales-invoices/{{invoiceId}}/pdf"),
            req("Inventory Reports", "GET", "/api/admin/inventory-reports"),
            req("Notifications", "GET", "/api/admin/notifications", query={"type": "All"}),
            req("Mark Notification Read", "PATCH", "/api/admin/notifications/{{notificationId}}/read"),
            req("Mark All Notifications Read", "PATCH", "/api/admin/notifications/read-all"),
            req("Get Profile", "GET", "/api/admin/profile"),
            req("Update Profile", "PUT", "/api/admin/profile", body={
                "fullName": "System Administrator", "businessEmail": "shop@autopartspro.com",
                "phoneNumber": "9800000000", "city": "Kathmandu",
            }),
            req("Upload Profile Photo", "POST", "/api/admin/profile/photo", formdata=True),
            req("Delete Profile Photo", "DELETE", "/api/admin/profile/photo"),
        ], "Requires Admin role"),
        folder("Staff Portal", [
            req("Get Profile", "GET", "/api/staff/profile"),
            req("Update Profile", "PUT", "/api/staff/profile", body={
                "fullName": "Staff User", "phoneNumber": "9800000003", "city": "Kathmandu",
            }),
            req("Upload Profile Photo", "POST", "/api/staff/profile/photo", formdata=True),
            req("Delete Profile Photo", "DELETE", "/api/staff/profile/photo"),
            req("Change Password", "POST", "/api/staff/change-password", body={
                "currentPassword": "Staff@123", "newPassword": "Staff@456", "confirmNewPassword": "Staff@456",
            }),
        ], "Staff-only for profile; change-password also allows Admin"),
        folder("Customers", [
            req("List Customers", "GET", "/api/customers", query={"search": ""}),
            req("Get Customer", "GET", "/api/customers/{{customerId}}"),
            req("Customer History", "GET", "/api/customers/{{customerId}}/history"),
            req("Create Customer", "POST", "/api/customers", body={
                "fullName": "Walk-in Customer", "phone": "9800000099", "email": "walkin@example.com",
                "city": "Kathmandu", "vehicles": [{
                    "numberPlate": "BA 2 PA 5678", "make": "Toyota", "model": "Corolla",
                    "year": 2019, "fuelType": "Petrol",
                }],
            }),
            req("Update Customer", "PUT", "/api/customers/{{customerId}}", body={
                "fullName": "Walk-in Customer", "phone": "9800000099", "email": "walkin@example.com", "city": "Bhaktapur",
            }),
            req("Delete Customer", "DELETE", "/api/customers/{{customerId}}"),
            req("Add Vehicle", "POST", "/api/customers/{{customerId}}/vehicles", body={
                "numberPlate": "BA 3 CH 1111", "make": "Suzuki", "model": "Swift", "year": 2021, "fuelType": "Petrol",
            }),
            req("Delete Vehicle", "DELETE", "/api/customers/{{customerId}}/vehicles/{{vehicleId}}"),
        ], "Admin + Staff"),
        folder("Parts", [
            req("List Parts", "GET", "/api/parts", query={"search": "", "inStock": "true"}),
            req("Get Part", "GET", "/api/parts/{{partId}}"),
            req("Create Part", "POST", "/api/parts", body={
                "partName": "Spark Plug", "description": "Iridium plug", "category": "Engine",
                "price": 450, "stockQuantity": 100, "vendorId": "{{vendorId}}",
            }),
            req("Update Part", "PUT", "/api/parts/{{partId}}", body={
                "partName": "Spark Plug", "description": "Iridium plug", "category": "Engine",
                "price": 475, "stockQuantity": 95, "vendorId": "{{vendorId}}",
            }),
            req("Upload Part Image", "POST", "/api/parts/{{partId}}/image", formdata=True),
            req("Delete Part Image", "DELETE", "/api/parts/{{partId}}/image"),
            req("Delete Part", "DELETE", "/api/parts/{{partId}}"),
        ], "Admin + Staff"),
        folder("Sales Invoices", [
            req("List Invoices", "GET", "/api/invoices", query={"search": ""}),
            req("Get Invoice", "GET", "/api/invoices/{{invoiceId}}"),
            req("Download Invoice PDF", "GET", "/api/invoices/{{invoiceId}}/pdf"),
            req("Create Invoice", "POST", "/api/invoices", body={
                "customerId": "{{customerId}}", "discountAmount": 0, "paymentStatus": "Paid",
                "items": [{"partId": "{{partId}}", "quantity": 1}],
            }),
            req("Send Invoice Email", "POST", "/api/invoices/{{invoiceId}}/send-email"),
        ], "Admin + Staff"),
        folder("Purchase Invoices", [
            req("List Purchase Invoices", "GET", "/api/purchase-invoices"),
            req("Get Purchase Invoice", "GET", "/api/purchase-invoices/{{purchaseInvoiceId}}"),
            req("Create Purchase Invoice", "POST", "/api/purchase-invoices", body={
                "vendorInvoiceNumber": "STF-VND-001", "vendorId": "{{vendorId}}",
                "purchaseDate": "2026-05-19T00:00:00Z",
                "items": [{"productName": "Air Filter", "quantity": 5, "unitPrice": 1200}],
            }),
            req("Download Purchase Invoice PDF", "GET", "/api/purchase-invoices/{{purchaseInvoiceId}}/pdf"),
        ], "Admin + Staff"),
        folder("Appointments", [
            req("List Appointments", "GET", "/api/appointments", query={"status": ""}),
            req("Slot Availability", "GET", "/api/appointments/slot-availability", query={"date": "2026-05-25", "time": "Morning (10 AM - 1 PM)"}),
            req("Accept Appointment", "POST", "/api/appointments/{{appointmentId}}/accept"),
            req("Propose Reschedule", "POST", "/api/appointments/{{appointmentId}}/propose-reschedule", body={
                "date": "2026-05-26", "time": "Afternoon (2 PM - 5 PM)", "message": "Please confirm new slot",
            }),
            req("Complete Appointment", "POST", "/api/appointments/{{appointmentId}}/complete"),
        ], "Admin + Staff"),
        folder("Part Requests", [
            req("List Part Requests", "GET", "/api/part-requests", query={"status": "", "escalatedOnly": "false"}),
            req("Escalate to Admin", "POST", "/api/part-requests/{{partRequestId}}/escalate-to-admin", body={"message": "Need admin approval"}),
            req("Set Availability", "POST", "/api/part-requests/{{partRequestId}}/set-availability", body={
                "date": "2026-06-01", "message": "Part will arrive by this date",
            }),
            req("Approve", "POST", "/api/part-requests/{{partRequestId}}/approve"),
            req("Reject", "POST", "/api/part-requests/{{partRequestId}}/reject", body={"message": "Cannot source this part"}),
            req("Request Vendor (Admin)", "POST", "/api/part-requests/{{partRequestId}}/request-vendor", body={
                "vendorId": "{{vendorId}}", "message": "Please supply", "quantity": 2,
            }),
            req("Record Vendor Invoice (Admin)", "POST", "/api/part-requests/{{partRequestId}}/record-vendor-invoice", body={
                "vendorInvoiceNumber": "VND-PR-001", "vendorId": "{{vendorId}}",
                "purchaseDate": "2026-05-20T00:00:00Z", "quantity": 2, "unitPrice": 3500,
            }),
        ], "Staff actions + Admin vendor workflow"),
        folder("Vendors", [
            req("List Vendors", "GET", "/api/vendors", query={"search": ""}),
            req("Get Vendor", "GET", "/api/vendors/{{vendorId}}"),
            req("Create Vendor (Admin)", "POST", "/api/vendors", body={
                "name": "New Vendor Ltd", "contactPerson": "Contact", "email": "vendor@example.com",
                "phone": "9800000100", "address": "Kathmandu",
            }),
            req("Update Vendor (Admin)", "PUT", "/api/vendors/{{vendorId}}", body={
                "name": "Updated Vendor", "contactPerson": "Contact", "email": "vendor@example.com",
                "phone": "9800000100", "address": "Lalitpur",
            }),
            req("Delete Vendor (Admin)", "DELETE", "/api/vendors/{{vendorId}}"),
        ]),
        folder("Notifications", [
            req("List Notifications", "GET", "/api/notifications", query={"type": "All"}),
            req("Notification Summary", "GET", "/api/notifications/summary"),
            req("Mark Read", "PATCH", "/api/notifications/{{notificationId}}/read"),
            req("Mark All Read", "PATCH", "/api/notifications/read-all"),
        ], "Admin + Staff (current user)"),
        folder("Reports", [
            req("Regular Customers", "GET", "/api/reports/regulars", query={"minimumInvoices": "3"}),
            req("High Spenders", "GET", "/api/reports/high-spenders", query={"threshold": "1000"}),
            req("Pending Credits", "GET", "/api/reports/pending-credits"),
        ], "Admin + Staff"),
        folder("Customer Portal", [
            req("Get Profile", "GET", "/api/customer/me"),
            req("Update Profile", "PUT", "/api/customer/me", body={
                "fullName": "Customer Name", "phoneNumber": "9800000001", "city": "Kathmandu",
            }),
            req("Change Password", "POST", "/api/customer/change-password", body={
                "currentPassword": "Test@123", "newPassword": "Test@456", "confirmNewPassword": "Test@456",
            }),
            req("Upload Profile Photo", "POST", "/api/customer/me/photo", formdata=True),
            req("Delete Profile Photo", "DELETE", "/api/customer/me/photo"),
            req("Add Vehicle", "POST", "/api/customer/vehicles", body={
                "numberPlate": "BA 4 GA 2222", "make": "Hyundai", "model": "i20", "year": 2022, "fuelType": "Petrol",
            }),
            req("Update Vehicle", "PUT", "/api/customer/vehicles/{{vehicleId}}", body={
                "numberPlate": "BA 4 GA 2222", "make": "Hyundai", "model": "i20", "year": 2022, "fuelType": "Diesel",
            }),
            req("Delete Vehicle", "DELETE", "/api/customer/vehicles/{{vehicleId}}"),
            req("List Appointments", "GET", "/api/customer/appointments"),
            req("Book Appointment", "POST", "/api/customer/appointments", body={
                "serviceType": "Full Service", "date": "2026-05-25", "time": "Morning (10 AM - 1 PM)",
                "notes": "Check brakes", "vehicle": "Honda City 2020 (BA 1 KA 1234)",
            }),
            req("Cancel Appointment", "DELETE", "/api/customer/appointments/{{appointmentId}}"),
            req("Accept Reschedule", "POST", "/api/customer/appointments/{{appointmentId}}/accept-reschedule"),
            req("Decline Reschedule", "POST", "/api/customer/appointments/{{appointmentId}}/decline-reschedule"),
            req("List Part Requests", "GET", "/api/customer/part-requests"),
            req("Create Part Request", "POST", "/api/customer/part-requests", body={
                "partName": "Alternator", "partDescription": "For Honda City 2020",
                "vehicleModel": "Honda City 2020", "urgencyLevel": "Medium (Within a week)",
            }),
            req("Cancel Part Request", "DELETE", "/api/customer/part-requests/{{partRequestId}}"),
            req("List Reviews", "GET", "/api/customer/reviews"),
            req("Submit Review", "POST", "/api/customer/reviews", body={
                "rating": 5, "title": "Great service", "content": "Fast delivery", "invoiceId": "{{invoiceId}}",
            }),
            req("List Notifications", "GET", "/api/customer/notifications"),
            req("Mark Notification Read", "PATCH", "/api/customer/notifications/{{notificationId}}/read"),
            req("Mark All Notifications Read", "POST", "/api/customer/notifications/read-all"),
            req("Delete Notification", "DELETE", "/api/customer/notifications/{{notificationId}}"),
            req("Browse Parts", "GET", "/api/customer/parts", query={"search": ""}),
            req("Get Part Detail", "GET", "/api/customer/parts/{{partId}}"),
            req("Checkout", "POST", "/api/customer/parts/checkout", body={
                "paymentStatus": "Paid", "items": [{"partId": "{{partId}}", "quantity": 1}],
            }),
            req("Purchase History", "GET", "/api/customer/purchases"),
            req("Download Purchase Invoice", "GET", "/api/customer/purchases/{{invoiceId}}/download"),
        ], "Requires Customer role"),
    ],
}

environment = {
    "id": "autopartspro-local-env",
    "name": "AutoPartsPro - Local",
    "values": [
        {"key": "baseUrl", "value": "http://localhost:5009", "type": "default", "enabled": True},
        {"key": "accessToken", "value": "", "type": "secret", "enabled": True},
        {"key": "adminEmail", "value": "admin@autopartspro.com", "type": "default", "enabled": True},
        {"key": "adminPassword", "value": "Admin@123", "type": "secret", "enabled": True},
        {"key": "staffEmail", "value": "", "type": "default", "enabled": True},
        {"key": "staffPassword", "value": "", "type": "secret", "enabled": True},
        {"key": "customerEmail", "value": "", "type": "default", "enabled": True},
        {"key": "customerPassword", "value": "", "type": "secret", "enabled": True},
        {"key": "customerId", "value": "", "type": "default", "enabled": True},
        {"key": "partId", "value": "", "type": "default", "enabled": True},
        {"key": "vendorId", "value": "", "type": "default", "enabled": True},
        {"key": "staffId", "value": "", "type": "default", "enabled": True},
        {"key": "partRequestId", "value": "", "type": "default", "enabled": True},
        {"key": "invoiceId", "value": "", "type": "default", "enabled": True},
        {"key": "appointmentId", "value": "", "type": "default", "enabled": True},
        {"key": "purchaseInvoiceId", "value": "", "type": "default", "enabled": True},
        {"key": "notificationId", "value": "", "type": "default", "enabled": True},
        {"key": "vehicleId", "value": "", "type": "default", "enabled": True},
    ],
    "_postman_variable_scope": "environment",
}

OUT.joinpath("AutoPartsPro.postman_collection.json").write_text(
    json.dumps(collection, indent=2), encoding="utf-8"
)
OUT.joinpath("AutoPartsPro-Local.postman_environment.json").write_text(
    json.dumps(environment, indent=2), encoding="utf-8"
)
print("Generated collection and environment in", OUT)
