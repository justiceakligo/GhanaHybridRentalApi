CREATE TABLE IF NOT EXISTS "DepositRefunds" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "Amount" numeric NOT NULL,
    "Currency" character varying(8) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "PaymentMethod" character varying(32) NOT NULL,
    "ExternalRefundId" character varying(256),
    "Reference" character varying(256),
    "RefundDetailsJson" text,
    "ProcessedByUserId" uuid,
    "CreatedAt" timestamp without time zone NOT NULL,
    "ProcessedAt" timestamp without time zone,
    "CompletedAt" timestamp without time zone,
    "DueDate" timestamp without time zone,
    "AdminNotified" boolean NOT NULL,
    "AdminNotifiedAt" timestamp without time zone,
    "ErrorMessage" text,
    "Notes" text,
    CONSTRAINT "PK_DepositRefunds" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DepositRefunds_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DepositRefunds_Users_ProcessedByUserId" FOREIGN KEY ("ProcessedByUserId") REFERENCES "Users" ("Id")
);

CREATE TABLE IF NOT EXISTS "EmailTemplates" (
    "Id" uuid NOT NULL,
    "TemplateName" character varying(128) NOT NULL,
    "Subject" character varying(256) NOT NULL,
    "BodyTemplate" text NOT NULL,
    "Description" text,
    "Category" character varying(50) NOT NULL,
    "IsActive" boolean NOT NULL,
    "IsHtml" boolean NOT NULL,
    "AvailablePlaceholdersJson" text,
    "CreatedAt" timestamp without time zone NOT NULL,
    "UpdatedAt" timestamp without time zone NOT NULL,
    "CreatedByUserId" uuid,
    CONSTRAINT "PK_EmailTemplates" PRIMARY KEY ("Id")
);
