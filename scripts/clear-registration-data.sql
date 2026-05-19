-- Removes Customer & Staff accounts and related registration data.
-- Keeps Admin users, vendors, parts, and purchase invoices.

BEGIN;

-- Sales data linked to customers/staff
DELETE FROM "SalesInvoiceItems"
WHERE "SalesInvoiceId" IN (
    SELECT "Id" FROM "SalesInvoices"
    WHERE "CustomerId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2))
       OR "StaffId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2))
);

DELETE FROM "SalesInvoices"
WHERE "CustomerId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2))
   OR "StaffId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2));

-- Customer-linked records
DELETE FROM "Reviews"
WHERE "CustomerId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2));

DELETE FROM "PartRequests"
WHERE "CustomerId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2));

DELETE FROM "Appointments"
WHERE "CustomerId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2));

DELETE FROM "Notifications"
WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2));

DELETE FROM "Vehicles"
WHERE "CustomerId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2));

DELETE FROM "StaffEmployments"
WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Role" IN (1, 2));

-- OTP rows for those emails (Role 1=Staff, 2=Customer)
DELETE FROM "OtpVerifications"
WHERE "Email" IN (SELECT "Email" FROM "Users" WHERE "Role" IN (1, 2));

DELETE FROM "Users"
WHERE "Role" IN (1, 2);

COMMIT;

SELECT "Role", COUNT(*) AS count FROM "Users" GROUP BY "Role" ORDER BY "Role";
