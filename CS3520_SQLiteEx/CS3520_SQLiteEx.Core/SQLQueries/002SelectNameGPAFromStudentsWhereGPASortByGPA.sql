-- Example Query
-- Selects some data from Students,
-- where GPA >= 3.0, order by GPA DESC
SELECT
    Name,
    GPA
FROM
    Students
WHERE
    GPA >= 3.0
ORDER BY
    GPA DESC