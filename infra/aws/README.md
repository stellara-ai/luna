# AWS Infrastructure

Infrastructure-as-Code for Luna deployment to AWS.

## Planned Setup

- **ECS Fargate**: Container orchestration for Luna.ApiGateway
- **RDS**: PostgreSQL database (per-module schema)
- **ElastiCache**: Redis for session state
- **ALB**: Application Load Balancer
- **ACM**: SSL certificates
- **CloudWatch**: Logging and monitoring

See `terraform/` or `cdk/` for implementation.
