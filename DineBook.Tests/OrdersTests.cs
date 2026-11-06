using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using Xunit;

namespace DineBook.Tests
{
    public class OrdersTests
    {
        private static Orders NewOrder() =>
            new Orders(1, 499m, "order_123", "session_abc", "key-0001", "buyer@example.com");

        [Fact]
        public void New_order_is_pending()
        {
            Assert.Equal(OrderStatus.Pending, NewOrder().Status);
        }

        [Fact]
        public void MarkAsPaid_changes_status_only_once()
        {
            var order = NewOrder();

            Assert.True(order.MarkAsPaid());
            Assert.False(order.MarkAsPaid()); // duplicate webhook or status check
            Assert.Equal(OrderStatus.Paid, order.Status);
        }

        [Fact]
        public void Paid_order_cannot_be_marked_failed()
        {
            var order = NewOrder();
            order.MarkAsPaid();

            Assert.Throws<DomainException>(() => order.MarkAsFailed());
        }

        [Fact]
        public void Zero_amount_is_rejected()
        {
            Assert.Throws<DomainException>(() => new Orders(1, 0m, "order_1", "session_1", "key-0002", "a@b.co"));
        }

        [Fact]
        public void Idempotency_key_is_required()
        {
            Assert.Throws<DomainException>(() => new Orders(1, 499m, "order_1", "session_1", " ", "a@b.co"));
        }

        [Fact]
        public void Only_paid_orders_can_be_refunded()
        {
            var order = NewOrder();

            Assert.Throws<DomainException>(() => order.RecordRefundRequest("rfnd_1"));
        }

        [Fact]
        public void Refund_flow_marks_order_refunded_once()
        {
            var order = NewOrder();
            order.MarkAsPaid();
            order.RecordRefundRequest("rfnd_1");

            Assert.True(order.MarkAsRefunded());
            Assert.False(order.MarkAsRefunded());
            Assert.Equal(OrderStatus.Refunded, order.Status);
        }

        [Fact]
        public void A_second_refund_id_is_rejected_while_one_is_in_progress()
        {
            var order = NewOrder();
            order.MarkAsPaid();
            order.RecordRefundRequest("rfnd_1");

            Assert.Throws<DomainException>(() => order.RecordRefundRequest("rfnd_2"));
        }

        [Fact]
        public void Cancelled_refund_allows_a_new_refund_id()
        {
            var order = NewOrder();
            order.MarkAsPaid();
            order.RecordRefundRequest("rfnd_1");

            order.ClearRefundRequest();
            order.RecordRefundRequest("rfnd_2");

            Assert.Equal("rfnd_2", order.RefundId);
        }
    }
}