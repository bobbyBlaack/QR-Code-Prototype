# JOI Prototype Database Schema

## Overview
This schema provides a normalized, production-ready structure with proper security, relationships, and audit capabilities.

---

## Core Tables

### 1. **ROLES** (Role Management)
Defines system roles and permissions.

```sql
CREATE TABLE roles (
  role_id INT PRIMARY KEY AUTO_INCREMENT,
  role_name VARCHAR(50) NOT NULL UNIQUE,
  description TEXT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

| role_id | role_name | description | created_at | updated_at |
|---------|-----------|-------------|------------|------------|
| 1 | Admin | Full system access | 2026-05-29 | 2026-05-29 |
| 2 | User | Standard user access | 2026-05-29 | 2026-05-29 |
| 3 | Logistics | Logistics operations only | 2026-05-29 | 2026-05-29 |

---

### 2. **USERS** (Normalized User Data)
Core user information with encrypted sensitive fields.

```sql
CREATE TABLE users (
  user_id VARCHAR(10) PRIMARY KEY,
  username VARCHAR(50) NOT NULL UNIQUE,
  email VARCHAR(100) NOT NULL UNIQUE,
  contact_number VARCHAR(20),
  password_hash VARCHAR(255) NOT NULL,
  role_id INT NOT NULL,
  is_active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  last_login TIMESTAMP NULL,
  FOREIGN KEY (role_id) REFERENCES roles(role_id),
  INDEX idx_email (email),
  INDEX idx_username (username)
);
```

| user_id | username | email | contact_number | password_hash | role_id | is_active | created_at | updated_at | last_login |
|---------|----------|-------|----------------|---------------|---------|-----------|------------|------------|------------|
| U001 | John123 | john@email.com | 0712345678 | $2b$12$... (hashed) | 1 | TRUE | 2026-05-29 | 2026-05-29 | 2026-05-29 |
| U002 | Sarah22 | sarah@email.com | 0723456789 | $2b$12$... (hashed) | 2 | TRUE | 2026-05-29 | 2026-05-29 | NULL |

---

### 3. **PACKAGES** (Package Information)
Replaces "Package Passwords" - stores metadata about packages.

```sql
CREATE TABLE packages (
  package_id INT PRIMARY KEY AUTO_INCREMENT,
  package_name VARCHAR(100) NOT NULL,
  description TEXT,
  package_type VARCHAR(50),
  status ENUM('active', 'inactive', 'archived') DEFAULT 'active',
  created_by VARCHAR(10) NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (created_by) REFERENCES users(user_id),
  INDEX idx_status (status),
  INDEX idx_package_type (package_type)
);
```

| package_id | package_name | description | package_type | status | created_by | created_at | updated_at |
|------------|--------------|-------------|--------------|--------|------------|------------|------------|
| 101 | Standard Package | Basic package for standard users | standard | active | U001 | 2026-05-29 | 2026-05-29 |
| 102 | Premium Package | Enhanced features for premium users | premium | active | U001 | 2026-05-29 | 2026-05-29 |
| 103 | Logistics Package | Special package for logistics team | logistics | active | U001 | 2026-05-29 | 2026-05-29 |

---

### 4. **PACKAGE_ACCESS** (Role-based Package Access)
Defines which roles can access which packages.

```sql
CREATE TABLE package_access (
  access_id INT PRIMARY KEY AUTO_INCREMENT,
  package_id INT NOT NULL,
  role_id INT NOT NULL,
  permission_level ENUM('read', 'write', 'admin') DEFAULT 'read',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (package_id) REFERENCES packages(package_id) ON DELETE CASCADE,
  FOREIGN KEY (role_id) REFERENCES roles(role_id) ON DELETE CASCADE,
  UNIQUE KEY unique_package_role (package_id, role_id),
  INDEX idx_role_id (role_id)
);
```

| access_id | package_id | role_id | permission_level | created_at |
|-----------|------------|---------|------------------|------------|
| 1 | 101 | 1 | admin | 2026-05-29 |
| 2 | 101 | 2 | read | 2026-05-29 |
| 3 | 102 | 1 | admin | 2026-05-29 |
| 4 | 102 | 2 | write | 2026-05-29 |
| 5 | 103 | 1 | admin | 2026-05-29 |
| 6 | 103 | 3 | write | 2026-05-29 |

---

### 5. **PACKAGE_CREDENTIALS** (Secure Package Access)
Stores encrypted credentials for package access.

```sql
CREATE TABLE package_credentials (
  credential_id INT PRIMARY KEY AUTO_INCREMENT,
  package_id INT NOT NULL,
  user_id VARCHAR(10),
  credential_key VARCHAR(255) NOT NULL,
  credential_value_encrypted LONGTEXT NOT NULL,
  expiry_date TIMESTAMP NULL,
  is_active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (package_id) REFERENCES packages(package_id) ON DELETE CASCADE,
  FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE SET NULL,
  INDEX idx_package_user (package_id, user_id),
  INDEX idx_expiry (expiry_date)
);
```

| credential_id | package_id | user_id | credential_key | credential_value_encrypted | expiry_date | is_active | created_at | updated_at |
|---------------|------------|---------|----------------|----------------------------|-------------|-----------|------------|------------|
| 1 | 101 | U001 | api_token | [encrypted] | 2027-05-29 | TRUE | 2026-05-29 | 2026-05-29 |
| 2 | 103 | U002 | access_key | [encrypted] | 2026-08-29 | TRUE | 2026-05-29 | 2026-05-29 |

---

### 6. **AUDIT_LOG** (Security & Compliance)
Tracks all user actions for security and compliance.

```sql
CREATE TABLE audit_log (
  log_id BIGINT PRIMARY KEY AUTO_INCREMENT,
  user_id VARCHAR(10),
  action VARCHAR(100) NOT NULL,
  resource_type VARCHAR(50),
  resource_id VARCHAR(50),
  details JSON,
  ip_address VARCHAR(45),
  status ENUM('success', 'failure') DEFAULT 'success',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE SET NULL,
  INDEX idx_user_created (user_id, created_at),
  INDEX idx_action (action),
  INDEX idx_created_at (created_at)
);
```

| log_id | user_id | action | resource_type | resource_id | details | ip_address | status | created_at |
|--------|---------|--------|---------------|-------------|---------|------------|--------|------------|
| 1 | U001 | LOGIN | user | U001 | {"device": "Chrome"} | 192.168.1.1 | success | 2026-05-29 |
| 2 | U001 | UPDATE_PACKAGE | package | 101 | {"field": "status", "old": "inactive", "new": "active"} | 192.168.1.1 | success | 2026-05-29 |

---

## Key Improvements

### Security
✅ **Password Hashing** - Passwords stored as hashes (bcrypt/Argon2), never plain text  
✅ **Credential Encryption** - Sensitive package credentials encrypted at rest  
✅ **Audit Trail** - All actions logged for compliance and forensics  
✅ **No Public Data** - Real emails/phones not in version control  

### Data Integrity
✅ **Foreign Keys** - Enforce referential integrity  
✅ **Unique Constraints** - Prevent duplicate usernames/emails  
✅ **Timestamps** - Track creation and modifications  
✅ **Status Fields** - Enable soft deletes and state management  

### Performance
✅ **Indexes** - On frequently queried columns (email, username, created_at)  
✅ **Cascade Deletes** - Clean up related records automatically  
✅ **Normalized Structure** - Reduces data redundancy  

### Scalability
✅ **Role-Based Access Control (RBAC)** - Granular permission management  
✅ **Separate Credentials Table** - Supports multiple credentials per package  
✅ **Audit Logging** - Supports compliance requirements  
