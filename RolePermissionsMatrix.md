# Role Permissions Matrix

This document provides a comprehensive overview of permissions across all workspaces and the roles that have access to them.

## Workspace Access

| Workspace | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer | System Administrator |
|-----------|-----------|---------|---------------------|--------------|---------------------|---------------------|
| My Research | ? | ? | ? | ? | ? | ? |
| Sponsor | ? | ? | ? | ? | ? | ? |
| Approvals | ? | ? | ? | ? | ? | ? |
| System Administration | ? | ? | ? | ? | ? | ? |

---

## My Research Workspace

### Project Records

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Create | Allows creating new project records | ? | ? | ? | ? | ? |
| Read | Allows reading project record details | ? | ? | ? | ? | ? |
| Update | Allows updating project records | ? | ? | ? | ? | ? |
| Delete | Allows deleting project records | ? | ? | ? | ? | ? |
| List | Allows viewing list of project records | ? | ? | ? | ? | ? |
| Search | Allows searching project records | ? | ? | ? | ? | ? |

### Project Record History

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Read | Allows reading project record history | ? | ? | ? | ? | ? |

### Project Documents

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Upload | Allows uploading project documents | ? | ? | ? | ? | ? |
| Update | Allows adding/updating documents metadata | ? | ? | ? | ? | ? |
| Review | Allows reviewing the details of project documents before the final action | ? | ? | ? | ? | ? |
| Delete | Allows deleting project documents | ? | ? | ? | ? | ? |
| Download | Allows downloading project documents | ? | ? | ? | ? | ? |
| List | Allows viewing the list of project documents | ? | ? | ? | ? | ? |

### Modifications

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Create | Allows creating modifications | ? | ? | ? | ? | ? |
| Read | Allows reading modification details | ? | ? | ? | ? | ? |
| Update | Allows updating modifications | ? | ? | ? | ? | ? |
| Delete | Allows deleting modifications | ? | ? | ? | ? | ? |
| List | Allows viewing list of modifications | ? | ? | ? | ? | ? |
| Search | Allows searching modifications | ? | ? | ? | ? | ? |
| Review | Allows reviewing modifications before the final action (i.e. sending to sponsor, approving) | ? | ? | ? | ? | ? |
| Approve | Allows approving modifications | ? | ? | ? | ? | ? |

### Modifications History

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Read | Allows reading modifications history | ? | ? | ? | ? | ? |
| BackStage Read | Allows reading modifications backstage history | ? | ? | ? | ? | ? |

---

## Sponsor Workspace

### Modifications

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Search | Allows searching modifications | ? | ? | ? | ? | ? |
| Review | Allows reviewing modifications before the final action (i.e. authorising, not authorising) | ? | ? | ? | ? | ? |
| Authorise | Allows authorising/not authorising modifications | ? | ? | ? | ? | ? |

---

## Approvals Workspace

### Project Records

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Search | Allows searching project records | ? | ? | ? | ? | ? |

### Modification Records

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Search | Allows searching modification records | ? | ? | ? | ? | ? |

### Modifications

| Permission | Description | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|------------|-------------|-----------|---------|---------------------|--------------|---------------------|
| Assign | Allows assigning modifications for review | ? | ? | ? | ? | ? |
| ReAssign | Allows re-assigning modifications for review | ? | ? | ? | ? | ? |

---

## Status-Based Access Control

### Project Record Statuses

| Status | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|--------|-----------|---------|---------------------|--------------|---------------------|
| In Draft | ? | ? | ? | ? | ? |
| Active | ? | ? | ? | ? | ? |

### Modification Statuses

| Status | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|--------|-----------|---------|---------------------|--------------|---------------------|
| In Draft | ? | ? | ? | ? | ? |
| With Sponsor | ? | ? | ? | ? | ? |
| With Review Body | ? | ? | ? | ? | ? |
| Approved | ? | ? | ? | ? | ? |
| Not Authorised | ? | ? | ? | ? | ? |
| Not Approved | ? | ? | ? | ? | ? |

### Document Statuses

| Status | Applicant | Sponsor | Workflow Coordinator | Team Manager | Study-wide Reviewer |
|--------|-----------|---------|---------------------|--------------|---------------------|
| Uploaded | ? | ? | ? | ? | ? |
| Failed | ? | ? | ? | ? | ? |
| Incomplete | ? | ? | ? | ? | ? |
| Complete | ? | ? | ? | ? | ? |
| With Sponsor | ? | ? | ? | ? | ? |
| With Review Body | ? | ? | ? | ? | ? |
| Approved | ? | ? | ? | ? | ? |
| Not Authorised | ? | ? | ? | ? | ? |
| Not Approved | ? | ? | ? | ? | ? |

---

## Legend

- ? = Permission granted
- ? = Permission not granted

## Notes

1. **System Administrator** role has full access to all workspaces but specific permissions are not defined in the RolePermissions class
2. The permissions are hierarchical following the pattern: `workspace.area.action`
3. Status-based permissions work in conjunction with action permissions to control access to records in specific states
4. Some workspaces (CAG Members, Member Management, CAT, REC Members, Technical Assurance, Technical Assurance Reviewers) are defined in the Permissions class but don't have role mappings yet
