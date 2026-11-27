using DineBook.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace DineBook.Domain.Specifications
{
    // Finds the payment attempt that Cashfree knows by this order id
    public sealed class PaymentByGatewayOrderIdSpecification(string gatewayOrderId) : ISpecification<Payment>
    {
        public Expression<Func<Payment, bool>> Criteria => payment => payment.GatewayOrderId == gatewayOrderId;
    }
}
