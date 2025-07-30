-- Add Process Server Status Column to Users Table
-- This script adds a boolean column to track whether a user is a process server

-- Add the process_server_status column to the users table
ALTER TABLE users 
ADD COLUMN process_server_status BOOLEAN DEFAULT FALSE;

-- Add a comment to document the column
COMMENT ON COLUMN users.process_server_status IS 'Indicates whether the user is a process server (TRUE) or not (FALSE)';

-- Update existing users to have process_server_status = FALSE by default
-- (This is already handled by the DEFAULT FALSE constraint, but we can be explicit)
UPDATE users 
SET process_server_status = FALSE 
WHERE process_server_status IS NULL;

-- Make the column NOT NULL after setting default values
ALTER TABLE users 
ALTER COLUMN process_server_status SET NOT NULL;

-- Create an index for better query performance when filtering by process server status
CREATE INDEX idx_users_process_server_status ON users(process_server_status);

-- Verify the changes
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'users' 
AND column_name = 'process_server_status';

-- Show sample data
SELECT 
    usernumber,
    loginname,
    firstname,
    lastname,
    enabled,
    process_server_status
FROM users 
ORDER BY loginname 
LIMIT 10; 