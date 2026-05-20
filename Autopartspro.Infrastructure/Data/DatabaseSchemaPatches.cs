using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Data;

/// <summary>
/// Idempotent SQL for columns that may be missing if manual migrations were not applied.
/// </summary>
public static class DatabaseSchemaPatches
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "ProposedDate" date;
            ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "ProposedTime" time without time zone;
            ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "StaffNotes" text;

            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "EstimatedAvailableDate" date;
            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "StaffNotes" text;
            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "StaffRespondedAt" timestamp with time zone;
            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "EscalatedAt" timestamp with time zone;

            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "MustChangePassword" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "BusinessEmail" text;

            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "VendorId" uuid;
            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "VendorRequestedAt" timestamp with time zone;
            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "VendorRequestMessage" text;
            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "PurchaseInvoiceId" uuid;
            ALTER TABLE "PartRequests" ADD COLUMN IF NOT EXISTS "InvoiceRecordedAt" timestamp with time zone;

            ALTER TABLE "SalesInvoices" ADD COLUMN IF NOT EXISTS "OverdueReminderSentAt" timestamp with time zone;
            ALTER TABLE "SalesInvoices" ADD COLUMN IF NOT EXISTS "VehicleId" uuid;

            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ProfileImageUrl" text;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ProfileImagePublicId" text;

            ALTER TABLE "Parts" ADD COLUMN IF NOT EXISTS "ImagePublicId" text;
            """,
            ct);
    }
}
