-- =====================================================
-- HOW TO: Manage Typing Speed Sample Texts
-- =====================================================
-- This guide shows database administrators how to add, update, 
-- view, and manage sample texts for typing speed assessments.
-- =====================================================

-- =====================================================
-- 1. VIEW ALL SAMPLE TEXTS
-- =====================================================
-- View all active sample texts
SELECT 
    id,
    LEFT(SampleText, 100) + '...' AS SampleTextPreview,
    LEN(SampleText) AS TextLength,
    IsActive,
    CreatedAt,
    CreatedBy,
    UpdatedAt,
    UpdatedBy
FROM TypingSpeedSampleTexts
WHERE IsActive = 1
ORDER BY id;

-- View all sample texts (including inactive)
SELECT 
    id,
    LEFT(SampleText, 100) + '...' AS SampleTextPreview,
    LEN(SampleText) AS TextLength,
    IsActive,
    CreatedAt,
    CreatedBy,
    UpdatedAt,
    UpdatedBy
FROM TypingSpeedSampleTexts
ORDER BY id;

-- View full text of a specific sample
SELECT 
    id,
    SampleText,
    LEN(SampleText) AS TextLength,
    IsActive,
    CreatedAt,
    CreatedBy
FROM TypingSpeedSampleTexts
WHERE id = 1; -- Replace with your sample text ID


-- =====================================================
-- 2. ADD NEW SAMPLE TEXT
-- =====================================================
-- Add a new typing speed sample text
-- Note: Keep texts between 200-400 characters for best results
INSERT INTO TypingSpeedSampleTexts (SampleText, IsActive, CreatedAt, CreatedBy)
VALUES (
    'Your sample text goes here. Make sure it is clear, professional, and appropriate for typing assessment. The text should be long enough to measure typing speed accurately but not so long that it becomes tedious.',
    1, -- IsActive: 1 = Active, 0 = Inactive
    GETDATE(),
    'AdminUsername' -- Replace with your username
);

-- Example: Add a technical sample text
INSERT INTO TypingSpeedSampleTexts (SampleText, IsActive, CreatedAt, CreatedBy)
VALUES (
    'Cloud computing has revolutionized how businesses operate in the digital age. Infrastructure as a Service, Platform as a Service, and Software as a Service models provide flexible and scalable solutions. Companies can now deploy applications globally with minimal physical infrastructure.',
    1,
    GETDATE(),
    'AdminUsername'
);


-- =====================================================
-- 3. UPDATE EXISTING SAMPLE TEXT
-- =====================================================
-- Update the text content of an existing sample
UPDATE TypingSpeedSampleTexts
SET 
    SampleText = 'Updated sample text content goes here.',
    UpdatedAt = GETDATE(),
    UpdatedBy = 'AdminUsername' -- Replace with your username
WHERE id = 1; -- Replace with the ID you want to update


-- =====================================================
-- 4. DEACTIVATE/REACTIVATE SAMPLE TEXT
-- =====================================================
-- Deactivate a sample text (soft delete - keeps in database but won't be used)
UPDATE TypingSpeedSampleTexts
SET 
    IsActive = 0,
    UpdatedAt = GETDATE(),
    UpdatedBy = 'AdminUsername'
WHERE id = 1; -- Replace with the ID you want to deactivate

-- Reactivate a sample text
UPDATE TypingSpeedSampleTexts
SET 
    IsActive = 1,
    UpdatedAt = GETDATE(),
    UpdatedBy = 'AdminUsername'
WHERE id = 1; -- Replace with the ID you want to reactivate


-- =====================================================
-- 5. DELETE SAMPLE TEXT (PERMANENT)
-- =====================================================
-- WARNING: This permanently deletes the sample text
-- Use deactivation (IsActive = 0) instead when possible
DELETE FROM TypingSpeedSampleTexts
WHERE id = 1; -- Replace with the ID you want to delete


-- =====================================================
-- 6. BULK INSERT SAMPLE TEXTS
-- =====================================================
-- Add multiple sample texts at once
INSERT INTO TypingSpeedSampleTexts (SampleText, IsActive, CreatedAt, CreatedBy)
VALUES 
    ('Sample text 1...', 1, GETDATE(), 'AdminUsername'),
    ('Sample text 2...', 1, GETDATE(), 'AdminUsername'),
    ('Sample text 3...', 1, GETDATE(), 'AdminUsername');


-- =====================================================
-- 7. VIEW STATISTICS
-- =====================================================
-- Get statistics about sample texts
SELECT 
    COUNT(*) AS TotalSamples,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveSamples,
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveSamples,
    AVG(LEN(SampleText)) AS AverageTextLength,
    MIN(LEN(SampleText)) AS MinTextLength,
    MAX(LEN(SampleText)) AS MaxTextLength
FROM TypingSpeedSampleTexts;


-- =====================================================
-- 8. FIND SAMPLE TEXTS BY KEYWORD
-- =====================================================
-- Search for sample texts containing specific keywords
SELECT 
    id,
    LEFT(SampleText, 100) + '...' AS SampleTextPreview,
    IsActive
FROM TypingSpeedSampleTexts
WHERE SampleText LIKE '%programming%' -- Replace with your keyword
  AND IsActive = 1;


-- =====================================================
-- 9. TEST RANDOM SELECTION
-- =====================================================
-- Test how the system randomly selects a sample text
-- Run this multiple times to see different results
SELECT TOP 1 
    id,
    SampleText,
    LEN(SampleText) AS TextLength
FROM TypingSpeedSampleTexts
WHERE IsActive = 1
ORDER BY NEWID();


-- =====================================================
-- BEST PRACTICES
-- =====================================================
-- 1. Keep sample texts between 200-400 characters
-- 2. Use professional, clear, and grammatically correct text
-- 3. Avoid special characters that are hard to type
-- 4. Include a mix of technical and general topics
-- 5. Maintain at least 5-10 active sample texts for variety
-- 6. Deactivate instead of delete to preserve history
-- 7. Always set CreatedBy/UpdatedBy for audit trail
-- 8. Test new sample texts for appropriate difficulty level
-- =====================================================
