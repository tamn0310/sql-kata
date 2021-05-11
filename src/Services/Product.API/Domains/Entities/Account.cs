using System;

namespace Product.API.Domains.Entities
{
    public class Account
    {
        public int Id { get; set; }

        /// <summary>
        /// the same id as company-id
        /// </summary>
        public int CompanyId { get; set; }

        /// <summary>
        /// default invoice date
        /// </summary>
        public string Name { get; set; }

        public string FullName { get; set; }

        public string TaxCode { get; set; }

        public string Address { get; set; }

        public string ContactName { get; set; }

        public string ContactPhone { get; set; }

        public string ContactEmail { get; set; }

        public string Remark { get; set; }

        /// <summary>
        /// yêu cầu xuất hóa đơn
        /// </summary>
        public bool VatInvoice { get; set; }


        public DateTime? BillingDate { get; set; }

        public bool? IsLocked { get; set; }

        public DateTime? LockedDate { get; set; }

        public DateTime? LockedEndDate { get; set; }

        public bool Inactive { get; set; }

        public DateTime LastModified { get; set; }
    }
}