# Product

Purpose: explain what TimeReady solves, for whom, and where the boundary of this repository sits.

## Problem

Before an employee goes on vacation, HR (or a line manager) still has to check the unglamorous parts:

- Is the time balance acceptable?
- Are enough vacation days left?
- Was the manager informed?
- Is the handover complete?

Those checks usually live in spreadsheets, emails and tribal knowledge. Mistakes show up late — often after the person has already left.

## Solution

TimeReady is a small leave and time-balance assistant. It stores the employee facts that matter for vacation preparation, runs an explicit readiness check, and surfaces what is still missing.

An employee is **Ready** when a vacation is planned and no critical finding is open. Warnings and info findings stay visible but do not block.

## Users and roles

| Role | Day-to-day work in this app |
| --- | --- |
| Operator | Review readiness, update employee preparation flags and balances |
| Admin | Everything an operator can do, plus create/delete employees and read the audit trail |

There is no self-service employee portal in this version. The primary user is HR (or a similarly responsible operator), not the person going on leave.

## In scope

- Employee CRUD with the fields the rule engine needs
- Rule-based readiness evaluation (thresholds from configuration)
- JWT authentication with Admin / Operator roles
- Append-only audit trail with retention/archiving
- Angular UI: login, dashboard, employees, notifications, audit (Admin)
- Docker Compose stack and CI for build/test

## Out of scope (deliberate)

- Payroll or legal time-tracking integration
- Calendar sync (Outlook, Google)
- Workflow approvals or e-mail notifications
- Multi-tenant SaaS / organisation hierarchy
- Machine learning or a paid AI API

The readiness engine is rule-based on purpose. Rules are readable, testable and explainable to HR. An optional later step could add an LLM that turns the same findings into a natural-language handover summary — without replacing the rules as the source of truth.

## Success criteria for this repository

1. A recruiter or engineer can run the full stack with `docker compose up --build`.
2. Demo data exercises every readiness rule at least once.
3. Auth, roles and audit behaviour are covered by automated tests.
4. Documentation states what the product does *and* what it deliberately does not do.
