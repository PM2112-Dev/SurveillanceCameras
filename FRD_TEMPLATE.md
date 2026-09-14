# Functional Requirements Document (FRD) - Template

**Project Name:** [Surveillance Cameras]

**Version:** 1.0  
**Date:** [26/08/2026]  
**Status:** Draft | In Review | Approved  
**Author:** [PDM]  
**Approved By:** [PDM]

---

## Table of Contents
1. [Introduction](#1-introduction)
2. [Functional Objectives](#2-functional-objectives)
3. [Non-Functional Objectives](#3-non-functional-objectives)
4. [The Context Model](#4-the-context-model)
5. [The Use Case Model](#5-the-use-case-model)
6. [Requirements Specification](#6-requirements-specification)
7. [Appendices](#7-appendices)

---

## 1. Introduction

### 1.1 Purpose of Document
Tài liệu phục vụ các mục đích:
- Làm cơ sở cho việc thiết kế và phát triển hệ thống
- Làm thỏa thuận giữa các bên liên quan về phạm vi và tính năng
- Làm căn cứ để kiểm thử và nghiệm thu sản phẩm
- Làm tài liệu tham chiếu cho quá trình bảo trì và mở rộng

### 1.2 Project Summary
*Mô tả tổng quan về dự án*

| Field | Description |
|-------|-------------|
| Project Name | Surveillance Cameras |
| Project Type | Web App |
| Target Users | [Nội bộ PDM] |
| Go-Live Date | [Ngày dự kiến] |
| Budget | [Ngân sách dự kiến] |

### 1.3 Background
**Vấn đề hiện tại:**

Hiện tại, quy trình quản lý và xử lý truyện gồm nhiều bước thủ công:

1. **Crawl thủ công:** Truy cập web nguồn truyện, copy thông tin truyện (title, author, description, image)
2. **Dịch nội dung:** Dịch máy word-by-word (dịch từng từ theo thứ tự gốc tiếng Trung), nội dung khó đọc và không tự nhiên → cần dịch lại bằng AI
3. **Đăng truyện:** Tạo story và upload từng chapter lên web đích thủ công
4. **Quản lý:** Theo dõi trạng thái nhiều truyện với nhiều chapters bằng spreadsheet hoặc ghi chú

**Hệ quả:**
- Tốn nhiều thời gian cho các bước lặp đi lặp lại
- Chất lượng dịch không đồng đều khi dịch thủ công
- Khó theo dõi tiến độ khi có nhiều truyện
- Khó quản lý nhiều tài khoản web đích

**Giải pháp:**

Hệ thống Quản lý Truyện Tự động nhằm tự động hóa toàn bộ quy trình:
- Tự động crawl thông tin truyện từ web nguồn
- Tự động dịch nội dung sang tiếng Việt tự nhiên bằng AI
- Tự động đăng truyện lên web đích
- Quản lý tập trung tài khoản và tiến độ xử lý

**Lợi ích kỳ vọng:**
- Giảm 80% thời gian xử lý mỗi truyện
- Đảm bảo chất lượng dịch thuật nhất quán
- Theo dõi tiến độ real-time
- Dễ dàng mở rộng thêm nền tảng đích mới

### 1.4 Project Scope

#### 1.4.1 In Scope (Thuộc phạm vi)
*Liệt kê các tính năng/chức năng sẽ được thực hiện trong dự án này*

| # | Feature | Description | Priority |
|---|---------|-------------|----------|
| 1 | Story Management | [Quản lý truyện] | Must Have |
| 2 | Chapter Management | [Quản lý chapter] | Must Have |
| 3 | Source Management | [Quản lý nguồn truyện] | Must Have |
| 4 | Platform Management | [Quản lý nền tảng up truyện] | Must Have |
| 5 | AI Management | [Quản lý AI] | Must Have |
| 6 | Account Management | [Quản lý tài khoản] | Must Have |
| 7 | Crawl Management | [Quản lý crawl] | Must Have |
| 8 | Translate Management | [Quản lý translate] | Must Have |
| 9 | Prompt Management | [Quản lý prompt] | Must Have |
| 10 | Billing Management | [Quản lý billing] | Must Have |
| 11 | Auto Process Management | [Quản lý auto process] | Must Have |

#### 1.4.2 Out of Scope (Không thuộc phạm vi)
*Những gì sẽ KHÔNG được thực hiện trong dự án này*

| # | Feature | Reason | Future Consideration |
|---|---------|--------|---------------------|
| 1 | [Video Management] | [Lý do không thuộc scope] | [Có thể xem xét trong phase tiếp theo] |
| 2 | [Image Management] | [Lý do không thuộc scope] | [Có thể xem xét trong phase tiếp theo] |

### 1.5 System Purpose

#### 1.5.1 Users
*Mô tả người dùng hệ thống*

| User Type | Role | Description | Number of Users |
|-----------|------|-------------|-----------------|
| Administrator | Admin | [Quản trị hệ thống] | [<5] |
| Manager | Manager | [Quản lý truyện] | [<10] |
| User | User | [Cập nhật và quản lý truyện] | [>100] |

#### 1.5.2 Location
*Địa điểm triển khai/người dùng*

| Location | Users | Time Zone |
|----------|-------|-----------|
| [Vietnam] | [100%] | [UTC+7] |

#### 1.5.3 Responsibilities
*Mô tả trách nhiệm của các bên liên quan*

| Stakeholder | Responsibilities |
|-------------|------------------|
| [PDM] | [Development and Management of the system] |

#### 1.5.4 Need
*Mô tả nhu cầu cần giải quyết*

| Need ID | Description | Priority | Current Solution |
|---------|-------------|----------|-----------------|
| N-01 | [Cần một hệ thống để quản lý truyện] | High | [Giải pháp hiện tại] |
<!-- | N-02 | [Cần một hệ thống để quản lý truyện] | Medium | [Giải pháp hiện tại] | -->

### 1.6 Overview of Document
*Mô tả cấu trúc của tài liệu này và cách đọc*

---

## 2. Functional Objectives

### 2.1 High Priority (Ưu tiên cao)
*Những tính năng bắt buộc phải có trong v1.0*

| Objective ID | Objective | Success Criteria |
|--------------|-----------|------------------|
| HO-01 | Story Management | [Tạo, cập nhật, xóa, tìm kiếm truyện] |
| HO-02 | Chapter Management | [Tạo, cập nhật, xóa, tìm kiếm chapter] |
| HO-03 | Source Management | [Tạo, cập nhật, xóa, tìm kiếm nguồn truyện] |
| HO-04 | Platform Management | [Tạo, cập nhật, xóa, tìm kiếm nền tảng truyện] |
| HO-05 | AI Management | [Tạo, cập nhật, xóa, tìm kiếm AI] |
| HO-06 | Account Management | [Tạo, cập nhật, xóa, tìm kiếm tài khoản] |
| HO-07 | Crawl Management | [Tạo, cập nhật, xóa, tìm kiếm crawl] |
| HO-08 | Translate Management | [Tạo, cập nhật, xóa, tìm kiếm translate] |
| HO-09 | Prompt Management | [Tạo, cập nhật, xóa, tìm kiếm prompt] |
| HO-10 | Billing Management | [Tạo, cập nhật, xóa, tìm kiếm billing] |
| HO-11 | Auto Process Management | [Tạo, cập nhật, xóa, tìm kiếm auto process] |

### 2.2 Medium Priority (Ưu tiên trung bình)
*Những tính năng quan trọng nhưng có thể delay*

| Objective ID | Objective | Success Criteria |
|--------------|-----------|------------------|
| MO-01 | [Objective] | [Criteria] |

### 2.3 Low Priority (Ưu tiên thấp)
*Những tính năng nice-to-have*

| Objective ID | Objective | Success Criteria |
|--------------|-----------|------------------|
| LO-01 | [Objective] | [Criteria] |

---

## 3. Non-Functional Objectives

### 3.1 Reliability
*Yêu cầu về độ tin cậy của hệ thống*

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Uptime | [X%] | [Method] |
| MTBF | [X hours] | [Method] |
| Recovery Time | [X minutes] | [Method] |

### 3.2 Usability
*Yêu cầu về tính dễ sử dụng*

| Metric | Target |
|--------|--------|
| Learning Time | [X hours] |
| Task Completion Rate | [X%] |
| User Satisfaction Score | [X/5] |

### 3.3 Performance
*Yêu cầu về hiệu năng*

| Metric | Target | Load Condition |
|--------|--------|----------------|
| Response Time | [X ms] | Normal |
| Response Time | [X ms] | Peak |
| Throughput | [X TPS] | Peak |
| Concurrent Users | [X] | - |

### 3.4 Security
*Yêu cầu về bảo mật*

| Requirement | Implementation |
|-------------|----------------|
| Authentication | [Method: JWT/OAuth/etc.] |
| Authorization | [RBAC/ABAC/etc.] |
| Data Encryption | [At rest: AES-256, In transit: TLS 1.3] |
| Password Policy | [Min X chars, complexity requirements] |
| Session Timeout | [X minutes] |
| Audit Logging | [All CRUD operations] |

### 3.5 Supportability
*Yêu cầu về khả năng hỗ trợ*

| Aspect | Requirement |
|--------|------------|
| Monitoring | [Tools: Prometheus/Grafana/etc.] |
| Logging | [Centralized logging] |
| Error Reporting | [Error tracking system] |
| Documentation | [API docs, User guide, Admin guide] |

### 3.6 Online User Documentation and Help
*Tài liệu hướng dẫn người dùng*

| Document | Format | Location |
|----------|--------|----------|
| User Guide | [PDF/HTML/Video] | [URL/Path] |
| API Documentation | [Swagger/OpenAPI] | [URL] |
| Admin Guide | [PDF] | [URL/Path] |
| FAQ | [HTML] | [URL] |

### 3.7 Purchased Components
*Các thành phần/thư viện được mua/licensed*

| Component | Vendor | License Type | Cost |
|-----------|--------|--------------|------|
| [Component 1] | [Vendor] | [Perpetual/Subscription] | [Cost] |
| [Component 2] | [Vendor] | [Perpetual/Subscription] | [Cost] |

### 3.8 Interfaces

#### 3.8.1 User Interfaces
*Mô tả các giao diện người dùng*

| Interface | Type | Description | Screen Count |
|-----------|------|-------------|--------------|
| [UI 1] | [Web/Mobile/Desktop] | [Description] | [X screens] |
| [UI 2] | [Web/Mobile/Desktop] | [Description] | [X screens] |

#### 3.8.2 External System Interfaces
*Các interface kết nối với hệ thống bên ngoài*

| System | Interface Type | Purpose | Data Flow |
|--------|---------------|---------|-----------|
| [System A] | [API/Webhook/File] | [Purpose] | [Inbound/Outbound] |
| [System B] | [API/Webhook/File] | [Purpose] | [Inbound/Outbound] |

---

## 4. The Context Model

### 4.1 Goal Statement
*Mục tiêu của hệ thống (1-2 câu)*

> [Statement]

### 4.2 Context Diagram
*Sơ đồ ngữ cảnh - các external entities và data flows*

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SYSTEM CONTEXT DIAGRAM                              │
└─────────────────────────────────────────────────────────────────────────────┘

                                    ┌─────────────┐
                                    │  External   │
                                    │  Entity A  │
                                    └──────┬──────┘
                                           │
                                           ▼
    ┌─────────────┐                  ┌─────────────┐                  ┌─────────────┐
    │  External   │                  │             │                  │  External   │
    │  Entity B   │─────────────────▶│   SYSTEM    │◀─────────────────│  Entity C   │
    └─────────────┘                  │             │                  └─────────────┘
                                     │  [Name]     │
                                     └──────┬──────┘
                                            │
                                            ▼
                                     ┌─────────────┐
                                     │  Database   │
                                     │  (if any)  │
                                     └─────────────┘
```

### 4.3 System Externals
*Mô tả chi tiết các external entities*

#### 4.3.1 [External Entity Name]

| Attribute | Description |
|-----------|-------------|
| Type | [Internal/External/Partner] |
| Description | [Mô tả] |
| Responsibilities | [Trách nhiệm] |
| Interfaces | [API/File/Manual] |
| Data Exchanged | [Danh sách data] |

#### 4.3.2 [External Entity Name]

| Attribute | Description |
|-----------|-------------|
| Type | [Internal/External/Partner] |
| Description | [Mô tả] |
| Responsibilities | [Trách nhiệm] |
| Interfaces | [API/File/Manual] |
| Data Exchanged | [Danh sách data] |

---

## 5. The Use Case Model

### 5.1 System Use Case Diagram
*Sơ đồ use case tổng quan*

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         USE CASE DIAGRAM                                   │
└─────────────────────────────────────────────────────────────────────────────┘

           ┌──────────┐                              ┌──────────┐
           │  Actor 1 │                              │  Actor 2 │
           └────┬─────┘                              └────┬─────┘
                │                                          │
                │  ┌─────────────────┐                     │
                │  │   Use Case 1    │                     │
                ├─▶│                 │◀────────────────────┤
                │  │   Description   │                     │
                │  └─────────────────┘                     │
                │                                          │
                │  ┌─────────────────┐                     │
                │  │   Use Case 2    │                     │
                ├─▶│                 │                     │
                │  │   Description   │                     │
                │  └─────────────────┘                     │
                │                                          │
                │  ┌─────────────────┐                     │
                │  │   Use Case 3    │                     │
                ├─▶│                 │                     │
                │  │   Description   │                     │
                │  └─────────────────┘                     │
                │                                          │
                │                              ┌─────────────────┐
                │                              │   Use Case 4    │
                └─────────────────────────────▶│                 │
                                               │   Description   │
                                               └─────────────────┘
```

### 5.2 Use Case Descriptions

#### 5.2.1 [Use Case Name]

| Field | Description |
|-------|-------------|
| **Use Case ID** | [UC-01] |
| **Use Case Name** | [Name] |
| **Priority** | [High/Medium/Low] |
| **Actors** | [Primary/Secondary actors] |
| **Description** | [Mô tả ngắn] |
| **Trigger** | [Sự kiện kích hoạt use case] |
| **Pre-conditions** | [Điều kiện trước khi bắt đầu] |
| **Post-conditions** | [Điều kiện sau khi kết thúc thành công] |
| **Normal Flow** | [Các bước chính] |
| **Alternative Flows** | [Các luồng thay thế] |
| **Exception Flows** | [Các luồng xử lý lỗi] |
| **Business Rules** | [Các business rules liên quan] |
| **Assumptions** | [Các giả định] |

**Normal Flow:**

| Step | Actor | System | Description |
|------|-------|--------|-------------|
| 1 | [Actor] | | [Action] |
| 2 | | [System] | [Response] |
| 3 | [Actor] | | [Action] |
| 4 | | [System] | [Response] |
| 5 | | | [End] |

**Alternative/Exception Flows:**

| Flow | Step | Condition | Action |
|------|------|-----------|--------|
| A1 | 3 | [Condition] | [Alternative action] |
| E1 | 4 | [Error] | [Error handling] |

---

#### 5.2.2 [Use Case Name]

| Field | Description |
|-------|-------------|
| **Use Case ID** | [UC-02] |
| **Use Case Name** | [Name] |
| **Priority** | [High/Medium/Low] |
| **Actors** | [Primary/Secondary actors] |
| **Description** | [Mô tả ngắn] |

*[Continue for all use cases]*

---

## 6. Requirements Specification

### 6.1 General Requirements
*Các yêu cầu chung áp dụng cho toàn bộ hệ thống*

| Req ID | Requirement | Priority | Category |
|--------|-------------|----------|-----------|
| GR-01 | [Requirement] | Must | Security |
| GR-02 | [Requirement] | Must | Performance |
| GR-03 | [Requirement] | Should | Usability |

### 6.2 Functional Requirements

#### 6.2.1 [Module/Subsystem Name]

| Req ID | Requirement | Input | Output | Business Rules | Priority |
|--------|-------------|-------|--------|----------------|----------|
| FR-01 | [Mô tả requirement] | [Input] | [Output] | [Rules] | Must |
| FR-02 | [Mô tả requirement] | [Input] | [Output] | [Rules] | Should |

#### 6.2.2 [Module/Subsystem Name]

| Req ID | Requirement | Input | Output | Business Rules | Priority |
|--------|-------------|-------|--------|----------------|----------|
| FR-03 | [Mô tả requirement] | [Input] | [Output] | [Rules] | Must |

### 6.3 Data Requirements

#### 6.3.1 Data Entities

| Entity Name | Description | Key Fields | Relationships |
|-------------|-------------|------------|---------------|
| [Entity 1] | [Description] | [Fields] | [Relations] |
| [Entity 2] | [Description] | [Fields] | [Relations] |

#### 6.3.2 Data Volume Estimates

| Entity | Current Volume | Growth Rate | Peak Volume |
|--------|---------------|-------------|--------------|
| [Entity 1] | [X records] | [Y%/year] | [Z records] |

### 6.4 API Requirements

#### 6.4.1 REST API Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| /api/v1/[resource] | GET | [Description] | Yes |
| /api/v1/[resource] | POST | [Description] | Yes |
| /api/v1/[resource]/{id} | PUT | [Description] | Yes |
| /api/v1/[resource]/{id} | DELETE | [Description] | Yes |

#### 6.4.2 API Request/Response Formats

**Request Format:**
```json
{
    "field1": "string (required)",
    "field2": "number (optional)",
    "field3": {
        "nested": "object"
    }
}
```

**Response Format (Success):**
```json
{
    "success": true,
    "data": { },
    "message": "Success"
}
```

**Response Format (Error):**
```json
{
    "success": false,
    "error": {
        "code": "ERROR_CODE",
        "message": "Human readable message"
    }
}
```

---

## 7. Appendices

### Appendix A: Glossary
*Định nghĩa các thuật ngữ*

| Term | Definition |
|------|------------|
| [Term 1] | [Definition] |
| [Term 2] | [Definition] |

### Appendix B: Abbreviations
*Các chữ viết tắt*

| Abbreviation | Full Form |
|--------------|-----------|
| [Abbr] | [Full Form] |
| [Abbr] | [Full Form] |

### Appendix C: References
*Các tài liệu tham khảo*

| Document | Location | Description |
|----------|----------|-------------|
| [Doc 1] | [URL/Path] | [Description] |
| [Doc 2] | [URL/Path] | [Description] |

### Appendix D: Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | [Date] | [Author] | Initial version |
| 1.1 | [Date] | [Author] | [Changes] |

---

*Document End*
