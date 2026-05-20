# AutoPartsPro — Postman API Documentation

Postman collection and environment for testing the full AutoPartsPro backend API.

## Files

| File | Purpose |
|------|---------|
| `AutoPartsPro.postman_collection.json` | All API requests (import into Postman) |
| `AutoPartsPro-Local.postman_environment.json` | Local variables (`baseUrl`, tokens, IDs) |
| `generate_collection.py` | Regenerates JSON after API changes |

## Quick start

1. **Start the API**
   ```bash
   cd Autopartspro/Autopartspro
   dotnet run --launch-profile http
   ```
   Base URL: **http://localhost:5009**

2. **Import in Postman**
   - **Import** → select both JSON files in this folder
   - Select environment **AutoPartsPro - Local** (top-right)

3. **Authenticate**
   - Open **Auth → Login (Admin)**
   - Default credentials (Development bootstrap):
     - Email: `admin@autopartspro.com`
     - Password: `Admin@123`
   - Send request → `accessToken` is saved automatically

4. **Call protected endpoints**
   - Collection uses **Bearer Token** auth with `{{accessToken}}`
   - Copy GUIDs from responses into environment variables (`customerId`, `partId`, etc.)

## Authentication

| Item | Value |
|------|--------|
| Type | JWT Bearer |
| Header | `Authorization: Bearer <token>` |
| Obtain token | `POST /api/auth/login` |
| Verify session | `GET /api/auth/me` |

### Login body

```json
{
  "email": "admin@autopartspro.com",
  "password": "Admin@123",
  "role": "Admin"
}
```

`role` must match the portal: `Admin`, `Staff`, or `Customer`.

### OTP flows (optional)

Some environments use OTP for email verification or login:

- `POST /api/auth/verify-email` — purpose: `EmailVerification`
- `POST /api/auth/verify-login` — purpose: `Login`
- `POST /api/auth/resend-otp`

## Environment variables

| Variable | Description |
|----------|-------------|
| `baseUrl` | API root (default `http://localhost:5009`) |
| `accessToken` | JWT (set by Login test script) |
| `adminEmail` / `adminPassword` | Admin login |
| `staffEmail` / `staffPassword` | Staff login |
| `customerEmail` / `customerPassword` | Customer login |
| `customerId`, `partId`, `vendorId`, … | GUIDs from list/create responses |

## API overview by folder

### Health
- `GET /api/health` — no auth

### Auth (`/api/auth`)
Public: register, login, OTP, forgot/reset password.  
Protected: `GET /api/auth/me`

### Admin (`/api/admin`) — **Admin only**
Dashboard, staff CRUD, parts (paginated), purchase invoices, financial reports, unpaid sales, inventory reports, notifications, profile + photo.

### Staff portal (`/api/staff`)
Profile + photo (**Staff**). Change password (**Staff** or **Admin**).

### Customers (`/api/customers`) — Admin, Staff
CRUD, history, vehicles.

### Parts (`/api/parts`) — Admin, Staff
CRUD, image upload (`multipart/form-data`, field `file`).

### Sales invoices (`/api/invoices`) — Admin, Staff
List, create, PDF, send email.

### Purchase invoices (`/api/purchase-invoices`) — Admin, Staff
List, create, PDF.

### Appointments (`/api/appointments`) — Admin, Staff
List, slot check, accept, propose reschedule, complete.

### Part requests (`/api/part-requests`) — Admin, Staff
Staff: escalate, set availability, approve, reject.  
Admin: request vendor, record vendor invoice.

### Vendors (`/api/vendors`)
GET: Admin + Staff. POST/PUT/DELETE: **Admin only**.

### Notifications (`/api/notifications`) — Admin, Staff
Current user’s notifications.

### Reports (`/api/reports`) — Admin, Staff
Regulars, high spenders, pending credits.

### Customer portal (`/api/customer`) — **Customer only**
Profile, vehicles, appointments, part requests, reviews, notifications, parts catalog, checkout, purchase history.

## File uploads

Use **Body → form-data**:

| Endpoint | Field |
|----------|--------|
| `POST /api/admin/profile/photo` | `file` |
| `POST /api/staff/profile/photo` | `file` |
| `POST /api/customer/me/photo` | `file` |
| `POST /api/parts/{id}/image` | `file` |

Max size: 5 MB. Types: JPEG, PNG, WebP, GIF.

## Recommended test flows

### Admin smoke
1. Health Check  
2. Login (Admin)  
3. Dashboard  
4. List Parts (paginated)  
5. Notifications  

### Staff sale
1. Login (Staff)  
2. List Customers → set `customerId`  
3. List Parts → set `partId`  
4. Create Invoice  
5. Download Invoice PDF  

### Customer journey
1. Register (Customer) → Verify Email (OTP)  
2. Login (Customer)  
3. Browse Parts → Checkout  
4. Purchase History  

### Part request workflow
1. Customer: Create Part Request  
2. Staff: List → Set Availability → Approve (or Escalate)  
3. Admin: Request Vendor → Record Vendor Invoice  

## HTTP status codes

| Code | Meaning |
|------|---------|
| 200 | OK |
| 201 | Created |
| 400 | Validation / bad request |
| 401 | Missing or invalid JWT |
| 403 | Wrong role |
| 404 | Not found |
| 409 | Conflict (e.g. insufficient stock) |

## Regenerating the collection

After adding or changing API endpoints:

```bash
python postman/generate_collection.py
```

Then re-import or replace the collection in Postman.

## Security

- Do not commit real passwords or production tokens.
- Use environment secrets for credentials; keep `appsettings.Development.json` out of git.
- Rotate any credentials shared in chat or docs.
