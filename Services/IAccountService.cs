using System;
using System.Threading.Tasks;

namespace FundWise.Services
{
    public interface IAccountService
    {
        /// <summary>
        /// Creates the initial MainAccount for a newly‑registered user.
        /// The balance is PKR 100 000.
        /// </summary>
        Task CreateMainAccountAsync(string userId);
    }
}
