-- =====================================================
-- TROUBLESHOOTING: Typing Speed Sample Texts Not Loading
-- =====================================================
-- Run these queries to diagnose why sample texts aren't loading
-- =====================================================

PRINT '========================================='
PRINT 'STEP 1: Check if table exists'
PRINT '========================================='

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TypingSpeedSampleTexts')
BEGIN
    PRINT '✓ Table TypingSpeedSampleTexts EXISTS'
END
ELSE
BEGIN
    PRINT '✗ ERROR: Table TypingSpeedSampleTexts DOES NOT EXIST!'
    PRINT '   ACTION: Run Risebird.DB\Tables\TypingSpeedSampleTexts.sql to create the table'
END

PRINT ''
PRINT '========================================='
PRINT 'STEP 2: Check if table has data'
PRINT '========================================='

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TypingSpeedSampleTexts')
BEGIN
    DECLARE @TotalCount INT = (SELECT COUNT(*) FROM TypingSpeedSampleTexts)
    DECLARE @ActiveCount INT = (SELECT COUNT(*) FROM TypingSpeedSampleTexts WHERE IsActive = 1)
    
    PRINT 'Total sample texts: ' + CAST(@TotalCount AS VARCHAR(10))
    PRINT 'Active sample texts: ' + CAST(@ActiveCount AS VARCHAR(10))
    
    IF @TotalCount = 0
    BEGIN
        PRINT '✗ ERROR: Table is EMPTY!'
        PRINT '   ACTION: Run the INSERT statements from TypingSpeedSampleTexts.sql to add default texts'
    END
    ELSE IF @ActiveCount = 0
    BEGIN
        PRINT '✗ WARNING: No ACTIVE sample texts!'
        PRINT '   ACTION: Update IsActive = 1 for at least one sample text'
    END
    ELSE
    BEGIN
        PRINT '✓ Table has ' + CAST(@ActiveCount AS VARCHAR(10)) + ' active sample text(s)'
    END
END

PRINT ''
PRINT '========================================='
PRINT 'STEP 3: View active sample texts'
PRINT '========================================='

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TypingSpeedSampleTexts')
BEGIN
    SELECT 
        id,
        LEFT(SampleText, 80) + '...' AS SampleTextPreview,
        LEN(SampleText) AS TextLength,
        IsActive,
        CreatedAt
    FROM TypingSpeedSampleTexts
    WHERE IsActive = 1
    ORDER BY id;
    
    IF NOT EXISTS (SELECT 1 FROM TypingSpeedSampleTexts WHERE IsActive = 1)
    BEGIN
        PRINT 'No active sample texts to display'
    END
END

PRINT ''
PRINT '========================================='
PRINT 'STEP 4: Check if stored procedure exists'
PRINT '========================================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetRandomTypingSpeedSampleText]') AND type in (N'P', N'PC'))
BEGIN
    PRINT '✓ Stored procedure usp_GetRandomTypingSpeedSampleText EXISTS'
END
ELSE
BEGIN
    PRINT '✗ ERROR: Stored procedure usp_GetRandomTypingSpeedSampleText DOES NOT EXIST!'
    PRINT '   ACTION: Run Risebird.DB\StoredProcedures\usp_GetRandomTypingSpeedSampleText.sql'
END

PRINT ''
PRINT '========================================='
PRINT 'STEP 5: Test stored procedure execution'
PRINT '========================================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetRandomTypingSpeedSampleText]') AND type in (N'P', N'PC'))
BEGIN
    BEGIN TRY
        EXEC usp_GetRandomTypingSpeedSampleText;
        PRINT '✓ Stored procedure executed successfully (see results above)'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR executing stored procedure:'
        PRINT 'Error: ' + ERROR_MESSAGE()
    END CATCH
END

PRINT ''
PRINT '========================================='
PRINT 'STEP 6: Verify sample text quality'
PRINT '========================================='

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TypingSpeedSampleTexts')
BEGIN
    SELECT 
        id,
        LEN(SampleText) AS TextLength,
        CASE 
            WHEN LEN(SampleText) < 100 THEN '⚠ Too short (< 100 chars)'
            WHEN LEN(SampleText) > 500 THEN '⚠ Too long (> 500 chars)'
            ELSE '✓ Good length'
        END AS LengthQuality,
        CASE 
            WHEN SampleText IS NULL OR LTRIM(RTRIM(SampleText)) = '' THEN '✗ Empty or NULL'
            ELSE '✓ Has content'
        END AS ContentQuality
    FROM TypingSpeedSampleTexts
    WHERE IsActive = 1;
END

PRINT ''
PRINT '========================================='
PRINT 'SUMMARY & NEXT STEPS'
PRINT '========================================='
PRINT ''
PRINT 'If any errors were found above:'
PRINT ''
PRINT '1. Table missing: Run Risebird.DB\Tables\TypingSpeedSampleTexts.sql'
PRINT '2. Stored procedure missing: Run Risebird.DB\StoredProcedures\usp_GetRandomTypingSpeedSampleText.sql'
PRINT '3. No data: Run the INSERT statements from TypingSpeedSampleTexts.sql'
PRINT '4. No active texts: Run UPDATE TypingSpeedSampleTexts SET IsActive = 1 WHERE id = [your_id]'
PRINT ''
PRINT 'After fixing database issues:'
PRINT '- Rebuild the backend solution'
PRINT '- Restart the application'
PRINT '- Check browser console for JavaScript errors'
PRINT '- Check application logs for detailed error messages'
PRINT ''
PRINT '========================================='

-- =====================================================
-- QUICK FIX: Add sample texts if table is empty
-- =====================================================
PRINT ''
PRINT 'Would you like to add default sample texts now?'
PRINT 'Uncomment and run the code below:'
PRINT ''
/*
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TypingSpeedSampleTexts')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM TypingSpeedSampleTexts)
    BEGIN
        INSERT INTO TypingSpeedSampleTexts (SampleText, IsActive, CreatedAt, CreatedBy)
        VALUES 
        ('The quick brown fox jumps over the lazy dog. Programming requires attention to detail and careful consideration of logic. Software development is both an art and a science that requires constant learning and adaptation.', 1, GETDATE(), 'System'),
        ('In the world of technology, innovation drives progress forward. Developers must balance creativity with practicality while building solutions that meet user needs. Effective communication and teamwork are essential skills in modern software development.', 1, GETDATE(), 'System'),
        ('Artificial intelligence and machine learning are transforming industries across the globe. These technologies enable computers to learn from data and make intelligent decisions. The future of computing lies in creating systems that can think and adapt like humans.', 1, GETDATE(), 'System'),
        ('Web development encompasses a wide range of technologies and frameworks. From frontend design to backend architecture, developers must master multiple disciplines. Building responsive and accessible applications requires dedication and continuous improvement.', 1, GETDATE(), 'System'),
        ('Data structures and algorithms form the foundation of computer science. Understanding how to efficiently store and manipulate data is crucial for building scalable systems. Problem-solving skills are developed through practice and perseverance.', 1, GETDATE(), 'System');
        
        PRINT '✓ Added 5 default sample texts successfully!'
    END
    ELSE
    BEGIN
        PRINT 'Table already has data. Skipping insert.'
    END
END
*/
