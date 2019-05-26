-- Example Query
-- Selects some data from Students,
-- LIMIT 10, Sort by BirthDate DESC
SELECT
    Name,
    BirthDate
FROM
    Students
ORDER BY
    BirthDate ASC
LIMIT 10