# Analysis

- Customers place orders that reduce item stock
- If stock is not enough, the order should be rejected
- Multiple orders for the same product can come from different customers, servers or services.
- Stock checks and decrements should be atomic to prevent overselling
- Consistency must be guaranteed
  - Isolation
  - Database lock
  - transaction
  - concurrency control