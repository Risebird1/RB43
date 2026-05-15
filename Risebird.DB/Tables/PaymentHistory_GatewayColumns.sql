-- Migration: Add Cashfree (and generic payment-gateway) columns to PaymentHistory
-- Safe to run multiple times (uses IF NOT EXISTS guards on each column).

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'PayoutReferenceId'
      AND Object_ID = OBJECT_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE [dbo].[PaymentHistory]
    ADD [PayoutReferenceId] VARCHAR(100) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'PaymentGateway'
      AND Object_ID = OBJECT_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE [dbo].[PaymentHistory]
    ADD [PaymentGateway] VARCHAR(50) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'GatewayStatus'
      AND Object_ID = OBJECT_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE [dbo].[PaymentHistory]
    ADD [GatewayStatus] VARCHAR(50) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'Fees'
      AND Object_ID = OBJECT_ID(N'PaymentHistory'))
BEGIN
    ALTER TABLE [dbo].[PaymentHistory]
    ADD [Fees] DECIMAL(18, 2) NULL DEFAULT 0;
END
GO
