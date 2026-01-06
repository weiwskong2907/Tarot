# Tarot Platform Project Development Plan

## 1. Project Initialization Phase
- [x] **Analysis**: Analyzed `Tarot Workflow.txt` v9.1.
- [x] **Directory Structure**:
    - `src/`: Core source code (Tarot.Api, Tarot.Core, Tarot.Infrastructure, Tarot.Worker).
    - `tests/`: Test projects (Tarot.Tests).
    - `docs/`: Documentation (Plans, API specs).
    - `config/`: Configuration files.
    - `scripts/`: Build and deployment scripts.
- [ ] **Environment Configuration**:
    - Git initialization with `.gitignore`.
    - `package.json` for frontend tooling (ESLint/Prettier).
    - .NET 8.0 SDK (using .NET 9.0 compatible env).

### Phase 2: Core Functionality (Weeks 2-3)
- [x] **Modular MVC Architecture**: Implement Controllers, Services, Repositories.
- [x] **Database Implementation**: PostgreSQL with EF Core, Migrations.
- [x] **Entities & Enums**: User, Service, Appointment, Consultation, etc.
- [x] **Authentication**: JWT, ASP.NET Core Identity.
- [x] **Automation Bot**: BackgroundService for auto-completion and cancellation.
- [x] **Testing Framework**: xUnit setup, Unit Tests for Services, Integration Tests.
- [x] **CI/CD Pipeline**: GitHub Actions workflow.

### Phase 3: Extended Functionality (Weeks 4-5)
- [x] **Plugin System**: Interface definition, PluginManager, Sample Plugin.
- [x] **Payment Integration**: Mock Payment Service implemented.
- [x] **Blog System**: CRUD for BlogPosts.
- [x] **API Extensions**: Advanced endpoints (Search, Filters).

## 4. Testing and Optimization Phase
- **Performance Testing**: JMeter/K6 for load testing.
- **Security**: OWASP ZAP scan.
- **Documentation**: Swagger/OpenAPI completeness.

## 5. Deployment and Maintenance Phase
- **Docker**: Create `Dockerfile` for Api and Worker.
- **CI/CD**: GitHub Actions workflow.
- **Monitoring**: HealthChecks, Prometheus metrics.

---
**Change Log Management**:
- All changes recorded in `logfile.txt`.

## Tarot工作流

```
## API 清单与 CRUD 覆盖
- Base URL: /api/v1
- Auth
  - POST /auth/register 实现于 [AuthController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AuthController.cs#L28-L57)
  - POST /auth/login 实现于 [AuthController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AuthController.cs#L59-L77)
- Services
  - GET /services 实现于 [ServicesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ServicesController.cs#L18-L30)
  - GET /services/{id} 实现于 [ServicesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ServicesController.cs#L32-L45)
  - POST /services 实现于 [ServicesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ServicesController.cs#L47-L69)
  - PUT /services/{id} 实现于 [ServicesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ServicesController.cs#L71-L89)
  - DELETE /services/{id} 实现于 [ServicesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ServicesController.cs#L91-L99)
- Slots
  - GET /slots?date=YYYY-MM-DD&serviceId=... 实现于 [SlotsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SlotsController.cs#L20-L63)
- Appointments (User)
  - POST /appointments 实现于 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L34-L80)
  - GET /appointments 实现于 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L82-L99)
  - GET /appointments/{id} 实现于 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L101-L118)
  - POST /appointments/{id}/cancel 实现于 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L120-L130)
  - POST /appointments/{id}/reschedule 实现于 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L132-L145)
  - POST /appointments/{id}/consultation 实现于 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L147-L184)
  - GET /appointments/{id}/calendar (.ics) 实现于 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L186-L205)
- Appointments (Admin)
  - POST /admin/appointments/{id}/reply 实现于 [AdminController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AdminController.cs#L58-L94)
- Interactive
  - POST /daily-draw 实现于 [InteractiveController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/InteractiveController.cs#L24-L63)
  - POST /self-reading 实现于 [InteractiveController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/InteractiveController.cs#L65-L112)
- Blog
  - GET /blogposts 实现于 [BlogPostsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/BlogPostsController.cs#L15-L24)
  - GET /blogposts/{slug} 实现于 [BlogPostsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/BlogPostsController.cs#L26-L40)
  - POST /blogposts 实现于 [BlogPostsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/BlogPostsController.cs#L42-L62)
  - PUT /blogposts/{id} 实现于 [BlogPostsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/BlogPostsController.cs#L64-L78)
  - DELETE /blogposts/{id} 实现于 [BlogPostsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/BlogPostsController.cs#L80-L87)
- Settings
  - PUT /settings/design 实现于 [SettingsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SettingsController.cs#L12-L34)
- Plugins
  - GET /plugins 实现于 [PluginsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/PluginsController.cs#L11-L20)
-
- Cards
  - GET /cards 实现于 [CardsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/CardsController.cs#L12-L30)
  - GET /cards/{id} 实现于 [CardsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/CardsController.cs#L32-L39)
  - POST /cards (KNOWLEDGE_EDIT) 实现于 [CardsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/CardsController.cs#L41-L59)
  - PUT /cards/{id} (KNOWLEDGE_EDIT) 实现于 [CardsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/CardsController.cs#L61-L77)
  - DELETE /cards/{id} (KNOWLEDGE_EDIT) 实现于 [CardsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/CardsController.cs#L79-L88)
- SiteSettings
  - GET /sitesettings (DESIGN_EDIT) 实现于 [SiteSettingsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SiteSettingsController.cs#L12-L27)
  - GET /sitesettings/{id} (DESIGN_EDIT) 实现于 [SiteSettingsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SiteSettingsController.cs#L29-L35)
  - GET /sitesettings/by-key/{key} (DESIGN_EDIT) 实现于 [SiteSettingsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SiteSettingsController.cs#L37-L44)
  - POST /sitesettings (DESIGN_EDIT) 实现于 [SiteSettingsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SiteSettingsController.cs#L46-L62)
  - PUT /sitesettings/{id} (DESIGN_EDIT) 实现于 [SiteSettingsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SiteSettingsController.cs#L64-L74)
  - DELETE /sitesettings/{id} (DESIGN_EDIT) 实现于 [SiteSettingsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SiteSettingsController.cs#L76-L84)
- EmailTemplates
  - GET /emailtemplates (DESIGN_EDIT) 实现于 [EmailTemplatesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/EmailTemplatesController.cs#L12-L28)
  - GET /emailtemplates/{id} (DESIGN_EDIT) 实现于 [EmailTemplatesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/EmailTemplatesController.cs#L30-L36)
  - GET /emailtemplates/by-slug/{slug} (DESIGN_EDIT) 实现于 [EmailTemplatesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/EmailTemplatesController.cs#L38-L45)
  - POST /emailtemplates (DESIGN_EDIT) 实现于 [EmailTemplatesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/EmailTemplatesController.cs#L47-L65)
  - PUT /emailtemplates/{id} (DESIGN_EDIT) 实现于 [EmailTemplatesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/EmailTemplatesController.cs#L67-L78)
  - DELETE /emailtemplates/{id} (DESIGN_EDIT) 实现于 [EmailTemplatesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/EmailTemplatesController.cs#L80-L88)
- ContactMessages
  - POST /contactmessages 实现于 [ContactMessagesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ContactMessagesController.cs#L12-L27)
  - GET /contactmessages (INBOX_MANAGE) 实现于 [ContactMessagesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ContactMessagesController.cs#L29-L45)
  - GET /contactmessages/{id} (INBOX_MANAGE) 实现于 [ContactMessagesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ContactMessagesController.cs#L47-L54)
  - PUT /contactmessages/{id}/reply (INBOX_MANAGE) 实现于 [ContactMessagesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ContactMessagesController.cs#L56-L66)
  - DELETE /contactmessages/{id} (INBOX_MANAGE) 实现于 [ContactMessagesController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/ContactMessagesController.cs#L68-L76)

## 软删除校验与覆盖
- 全局过滤: 已在 [AppDbContext.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Infrastructure/Data/AppDbContext.cs#L22-L36) 为所有主要实体启用 DeletedAt==null 过滤。
- 仓储删除: 已将通用仓储删除改为软删除，见 [EfRepository.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Infrastructure/Data/EfRepository.cs#L33-L53) 的 DeleteAsync，当实体包含 DeletedAt 字段时设置当前时间并保存。
- 实体字段: 迁移快照显示所有核心表包含 DeletedAt 字段，如 [AppDbContextModelSnapshot.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Infrastructure/Migrations/AppDbContextModelSnapshot.cs#L239-L279) 和 [UpdateEntities.Designer.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Infrastructure/Migrations/20260105191242_UpdateEntities.Designer.cs#L242-L279)。

## 与《Tarot Workflow.txt》一致性检查结论
- 预约闭环
  - 查询时段: 已提供 GET /slots，支持预约与闭锁叠加计算，详见 [SlotsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/SlotsController.cs#L20-L63)。
  - 下单与支付: POST /appointments 按环境变量 ENABLE_PAYMENT 控制支付流程，详见 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L34-L64)。
  - 咨询: POST /appointments/{id}/consultation 已实现，详见 [AppointmentsController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AppointmentsController.cs#L147-L184)。
  - 管理员回复与完结: POST /admin/appointments/{id}/reply 已实现，详见 [AdminController.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Controllers/AdminController.cs#L58-L94)。
  - 自动化机器人: 后台任务在 [AppointmentCleanupWorker.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Worker/AppointmentCleanupWorker.cs#L64-L129) 中实现自动取消/自动完结与积分发放。
- 权限策略
  - 使用 AddAuthorizationBuilder 注册策略，参见 [Program.cs](file:///c:/Users/User/Desktop/Tarot/src/Tarot.Api/Program.cs)。
- 系统配置
  - 设计配置 PUT /settings/design 已实现。
- 互动功能
  - 每日一抽与自助占卜均已实现，见 InteractiveController。

结论：当前代码已覆盖工作流列出的核心 API，并为 Services 与 BlogPosts 暴露了完整 CRUD。删除操作统一为软删除，查询默认过滤已软删除数据。
塔罗牌预约与咨询全功能平台 - 全栈开发规格说明书 v9.1 (C# .NET Edition)
文档状态: Final / Ready for Development 技术栈: C# .NET 8.0 Only 文档目的: 为开发团队提供单一的、无歧义的执行标准，涵盖架构、逻辑、数据与界面。
0. 架构与技术栈 (Architecture & Tech Stack)
0.1 核心架构模式
系统严格遵循 MVC (Model-View-Controller) 设计模式，确保高内聚低耦合。
Model: 使用 Entity Framework Core 定义数据实体与业务逻辑规则。
View: 前端 UI 渲染、PWA 离线缓存、用户交互。
Controller: .NET Web API 控制器，负责路由分发与请求处理。
0.2 技术选型 (Tech Stack)
Backend: C# (.NET 8.0 Web API).
ORM: Entity Framework Core (Code-First approach).
Auth: ASP.NET Core Identity + JWT Bearer Authentication.
Database: PostgreSQL 14+ (需开启事务支持).
Cache & Queue: Redis (用于会话、防爆破锁、分布式缓存).
Scheduler: BackgroundService (IHostedService) 或 Hangfire (用于定时任务).
Mail: MailKit (SMTP) + RazorLight (邮件模板渲染).
Calendar: iCal.NET (生成 .ics 日历文件).
Health Check: AspNetCore.Diagnostics.HealthChecks.
API Docs: Swagger (Swashbuckle).
Frontend: HTML5, CSS3, JavaScript (ES6+), AJAX, SweetAlert2, Chart.js.
Mobile: PWA (Progressive Web App) manifest & service workers.
0.3 基础设施 (Infrastructure)
Web Server: Nginx (Linux 反向代理) + Kestrel (应用服务器).
Security: Cloudflare (DNS, CDN, WAF, DDoS Protection).
Email: Gmail SMTP (配置 App Password).
DevOps: Docker + GitHub Actions (CI/CD).
1. 用户角色与权限 (Roles & Permissions)
1.1 角色定义
Customer (客户): 注册用户。拥有浏览、预约、支付、咨询、查看个人历史、每日抽牌权限。
Admin (塔罗师/员工): 需 Super Admin 授权。基础权限为登录后台。具体操作权限由 Permission Flags 控制。
Super Admin (站长): 拥有系统最高权限（包括设计网站、查看审计日志、管理人员）。
1.2 动态权限标识 (Permission Flags)
用于后端 [Authorize(Policy = "...")] 校验和前端按钮显隐。
SCHEDULE_MANAGE: 排期管理（修改时段、闭锁）。
CONSULTATION_REPLY: 回复客户咨询。
FINANCE_VIEW: 查看财务报表。
BLOG_MANAGE: 发布与编辑文章。
KNOWLEDGE_EDIT: 修改塔罗牌意库。
DESIGN_EDIT: 修改网站外观 (Super Admin Only)。
2. 核心业务逻辑 (Core Business Logic)
A. 预约与咨询一体化 (The Booking Loop)
查询: 用户选择日期 -> 后端计算可用时段 (Total - Booked - Blocked - Holiday).
下单: 用户选定服务与时间 -> 创建订单 (Status: Pending) -> 锁定库存 (Redis Lock).
支付:
若 ENABLE_PAYMENT=true: 跳转 Stripe/PayPal -> 回调成功 -> Status: Confirmed -> Payment: Paid.
若 ENABLE_PAYMENT=false: 直接转为 Status: Confirmed -> Payment: Skipped.
咨询: 订单详情页内嵌聊天窗口。用户提问 -> 状态变 In Progress.
履约: Admin 拍照上传牌阵 -> 填写文字 -> 提交 -> 状态变 Completed.
完结: 发送邮件通知 -> 发放积分 -> 邀请评价。
B. 自动化机器人 (Automation Bot)
后台守护进程 (BackgroundService)，每 1 分钟运行一次。
Task 1: Auto-Complete (自动完结)
Condition: EndTime < Now - 10min AND Admin 未回复.
Action: 强制 Status: Completed -> 标记 AutoCompleted=true -> 发放积分 -> 记日志。
Task 2: Auto-Cancel (超时取消)
Condition: CreatedTime < Now - 15min AND Status: Pending AND ENABLE_PAYMENT=true.
Action: 强制 Status: Cancelled -> 释放库存 -> 发邮件通知。
C. 会员与积分 (Loyalty System)
累进规则:
第 1-5 单: 1.0x 积分.
第 6-10 单: 1.5x 积分.
第 11+ 单: 2.0x 积分.
安全: 积分发放操作必须在数据库事务 (Transaction) 中执行。
3. 数据库设计 (Database Schema)
约定: 所有表包含 Id (Guid), CreatedAt, UpdatedAt, DeletedAt (Soft Delete).
3.1 用户与权限
Users:
Email (UQ), PasswordHash, Role (Enum).
LoyaltyPoints (int), AppointmentCount (int).
Permissions (JSONB): e.g., ["SCHEDULE_MANAGE", "BLOG_EDIT"].
Tags (JSONB): e.g., ["VIP", "HighSpender"].
3.2 业务核心
Services: Name, Price, DurationMin, IsActive.
Appointments:
UserId (FK), ServiceId (FK).
StartTime, EndTime (DateTimeOffset).
Status (Enum: Pending, Confirmed, InProgress, Completed, Cancelled).
PaymentStatus (Enum: Unpaid, Paid, Refunded, Skipped).
MeetingLink (text), CancellationReason (text).
AutoCompletedAt (DateTimeOffset?), RescheduleCount (int).
Consultations:
AppointmentId (FK, UQ).
Question (text), UserImages (JSONB).
Reply (text), ReplyImages (JSONB), RepliedAt.
3.3 互动与内容
Cards:
Name, ImageUrl, Suit, ArcanaType.
MeaningUpright (text), MeaningReversed (text).
Keywords (JSONB), AdminNotes (text).
DailyDrawRecords: UserId (FK), CardId (FK), DrawDate, Notes.
SelfReadings: UserId (FK), QuestionCategory, CardsJson (Array of {CardId, Position, Reversed}).
BlogPosts: Title, Slug (UQ), Content, SeoMeta (JSONB).
3.4 系统配置与运营
SiteSettings: Key (PK), Value (JSONB).
e.g. design_config, enable_payment.
EmailTemplates: Slug (UQ), SubjectTpl, BodyHtml.
ContactMessages: Name, Email, Message, Reply, Status.
AuditLogs: ActorId, Action, Details, IpAddress.
4. 前端页面清单与开发优先级 (Frontend Page List & Priorities)
图例: 🟢 P1 (MVP 核心闭环) | 🔵 P2 (运营增长)
4.1 公开页面 (Public)
页面名称	路由	优先级	功能描述
首页	/	🟢 P1	Hero Banner, 热门服务, 客户评价 (P2加每日一抽板块).
服务列表	/services	🟢 P1	卡片展示服务价格、时长、简介.
联系我们	/contact	🟢 P1	留言表单 (AJAX 提交).
登录/注册	/login	🟢 P1	用户认证, 忘记密码.
塔罗牌意库	/cards	🔵 P2	78张牌搜索与列表.
牌意详情	/cards/{id}	🔵 P2	单张牌高清图与含义.
每日一抽	/daily-draw	🔵 P2	互动：洗牌动画、抽牌、生成今日运势.
自助占卜室	/reading-room	🔵 P2	免费三张牌占卜 (Past/Present/Future).
博客列表	/blog	🔵 P2	文章列表 (SEO).

4.2 客户专用 (Customer)

页面名称	路由	优先级	功能描述
仪表盘	/dashboard	🟢 P1	积分概览，即将开始的预约提醒.
预约日历	/my-appointments	🟢 P1	[Core] 日历视图展示订单. 点击查看详情.
预约详情	/appointments/{id}	🟢 P1	[Core] 查看状态、发起咨询(Chat)、上传照片、评价.
预约-选择	/book	🟢 P1	Step 1: 选择服务 -> 日期 -> 时段 (AJAX Fetch Slots).
预约-确认	/book/confirm	🟢 P1	Step 2: 支付/优惠码/确认.
预约成功	/book/success	🟢 P1	显示 "Add to Calendar" 按钮.
个人设置	/settings	🟢 P1	修改密码/资料.
我的旅程	/my-journey	🔵 P2	历史归档：每日一抽记录、自助占卜记录.
4.3 管理员 (Admin)

页面名称	路由	优先级	功能描述
工作台	/admin	🟢 P1	[Core] 今日待办任务列表 (Today's Tasks).
排期管理	/admin/schedule	🟢 P1	日历视图，设置“闭锁 (Block)”.
订单列表	/admin/orders	🟢 P1	表格视图，支持筛选.
订单回复	/admin/orders/{id}	🟢 P1	[Core] 详情页. 聊天框、拍照上传、虚拟抽牌入口.
留言信箱	/admin/inbox	🟢 P1	处理 Contact Us 留言, 邮件回复.
知识库管理	/admin/cards	🔵 P2	编辑牌意、添加私人笔记.
文章管理	/admin/blog	🔵 P2	发布/编辑博客 (Rich Text Editor).
4.4 超级管理员 (Super Admin)
页面名称	路由	优先级	功能描述
人员权限	/admin/staff	🟢 P1	创建 Admin，勾选权限.
全局设置	/admin/settings	🟢 P1	支付开关、邮件模板编辑.
健康看板	/admin/health	🔵 P2	系统状态监控 (DB/Redis/Disk).
外观设计	/admin/design	🔵 P2	可视化修改 Logo/配色/布局.
财务报表	/admin/finance	🔵 P2	收入统计图表 (JS Graph).
回收站	/admin/trash	🔵 P2	数据恢复.
审计日志	/admin/audit	🔵 P2	操作记录查询.
5. 移动端适配指南 (Mobile Adaptation)
A. 客户视角 (Customer View)
导航: 使用底部标签栏 (Bottom Tab Bar): 首页 | 预约 | 我的旅程 | 我的。
预约流程: 日历和时段选择在手机上需 垂直堆叠 显示。
PWA: 支持添加到主屏幕，隐藏浏览器地址栏。
B. 管理员视角 (Admin View)
Dashboad: 手机首屏直接展示 Today's Tasks 列表卡片。
履约优化: 在 Order Reply 页面，底部悬浮大尺寸 [📷 拍照上传] 按钮，直接调用手机摄像头。
6. API 接口规范 (API Endpoints)
Base URL: /api/v1
Auth
POST /auth/login (Return JWT)
POST /auth/register
Booking (User)
GET /services
GET /slots?date=2024-01-01 (Return available times)
POST /appointments (Create)
GET /appointments (List)
GET /appointments/{id} (Detail)
POST /appointments/{id}/reschedule
POST /appointments/{id}/consultation (User sends msg/image)
Booking (Admin)
POST /admin/appointments/{id}/reply (Admin sends msg/image/virtual_card)
POST /admin/appointments/{id}/cancel
POST /admin/slots/block
Interactive
POST /daily-draw
POST /self-reading
System
GET /health (Health Check)
PUT /settings/design (Design Config)
7. 目录结构规范 (Project Structure)
/TarotProject.Solution
├── /src
│   ├── /Tarot.Api              
│   │   ├── Controllers/ 
│   │   │   ├── HealthCheckController.cs 
│   │   │   ├── AdminManageController.cs 
│   │   │   ├── DesignController.cs 
│   │   ├── Program.cs          
│   ├── /Tarot.Core             
│   ├── /Tarot.Infrastructure   
│   │   ├── Data/DataSeeder.cs  
│   │   ├── Services/CalendarService.cs 
│   │   ├── Services/SystemDiagnosticsService.cs 
│   ├── /Tarot.Worker           # BackgroundService for Automation Bot
│   └── /Tarot.Tests
8. 安全与运维 (Security & Ops)
基础设施: Cloudflare (WAF/DDoS) + UFW 防火墙 (Only 80/443/22).
数据安全:
软删除: Entity Framework Global Query Filter 实现 DeletedAt 过滤。
备份: 每日自动备份数据库至外部存储。
应用安全:
输入验证: 全局防 SQL 注入，XSS 过滤。
文件上传: 校验文件 Magic Bytes，限制图片大小，随机重命名。
权限: 严格的 RBAC 校验，Token 验证。
初始化: 系统启动时检测 Users 表，若空则读取环境变量自动创建默认 Super Admin。
```
