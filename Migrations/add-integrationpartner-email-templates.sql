-- Add IntegrationPartner email templates (application received/approved/rejected)
-- Run during deployment after database is ready

DELETE FROM "EmailTemplates" WHERE "TemplateName" IN (
    'integration_partner_application_received',
    'integration_partner_application_approved',
    'integration_partner_application_rejected'
);

INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'integration_partner_application_received',
    'IntegrationPartner Application Received - {{application_reference}}',
    '<!DOCTYPE html><html><body><p>Hi {{contact_person}},</p><p>Thanks for applying to become a RyveRental IntegrationPartner.</p><p>Your application reference: <strong>{{application_reference}}</strong></p><p>We will review your application and contact you within {{review_time}}.</p><p>If you have questions, contact us at {{support_email}}.</p><p>Best regards,<br/>RyveRental Team</p></body></html>',
    'partner',
    'Notifies applicant that the IntegrationPartner application was received',
    true,
    true,
    '["contact_person","business_name","application_reference","submitted_at","review_time","support_email"]',
    NOW(),
    NOW()
);

INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'integration_partner_application_approved',
    'Welcome to RyveRental IntegrationPartner Program - {{business_name}}',
    '<!DOCTYPE html><html><body><p>Hi {{contact_person}},</p><p>Great news — your IntegrationPartner application ({{application_reference}}) for <strong>{{business_name}}</strong> has been approved.</p><p><strong>API Key:</strong> <code>{{api_key}}</code></p><p><strong>Key Expires:</strong> {{api_key_expires_at}}</p><p>Get started with our docs: <a href="{{docs_url}}">{{docs_url}}</a></p><p>If you need help, contact {{support_email}}.</p><p>⚠️ Save your API key now. It will not be shown again.</p><p>Best regards,<br/>RyveRental Team</p></body></html>',
    'partner',
    'Sends API key and welcome info when IntegrationPartner application is approved',
    true,
    true,
    '["contact_person","business_name","application_reference","api_key","api_key_expires_at","docs_url","support_email"]',
    NOW(),
    NOW()
);

INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'integration_partner_application_rejected',
    'IntegrationPartner Application Update - {{application_reference}}',
    '<!DOCTYPE html><html><body><p>Hi,</p><p>Thank you for applying for the RyveRental IntegrationPartner program.</p><p>After review, your application ({{application_reference}}) could not be approved at this time.</p><p>Reason: {{rejection_reason}}</p><p>You are welcome to reapply once the issue has been resolved. Contact {{support_email}} for help.</p><p>Best regards,<br/>RyveRental Team</p></body></html>',
    'partner',
    'Sends rejection reason to IntegrationPartner applicant',
    true,
    true,
    '["application_reference","rejection_reason","support_email"]',
    NOW(),
    NOW()
);
