using Autopartspro.Application.DOTs.auth;
using Autopartspro.Application.DOTs.customer;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Autopartspro.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Autopartspro.Tests
{
    public class PortionTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Feature12_Customer_SelfRegister_And_Manage()
        {
            // Arrange
            var dbContext = GetDbContext();
            var mockJwtService = new Mock<IJwtService>();
            var mockOtpService = new Mock<IOtpService>();
            var mockEmailService = new Mock<IEmailService>();

            mockOtpService.Setup(s => s.GenerateAndSendOtpAsync(It.IsAny<string>(), It.IsAny<OtpPurpose>()))
                .ReturnsAsync("OTP Sent");
            mockOtpService.Setup(s => s.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
                .ReturnsAsync(true);
            mockJwtService.Setup(s => s.GenerateToken(It.IsAny<User>()))
                .Returns("mock_token");

            var authService = new AuthService(dbContext, mockJwtService.Object, mockOtpService.Object, mockEmailService.Object);
            var customerService = new CustomerService(dbContext);

            // Act 1: Register Customer
            var registerDto = new RegisterDto
            {
                Role = "Customer",
                FullName = "Test User",
                Email = "test@user.com",
                PhoneNumber = "9800000000",
                City = "Kathmandu",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                AgreeToTerms = true,
                Make = "Honda",
                Model = "Civic",
                Year = 2022,
                FuelType = "Petrol",
                NumberPlate = "BA 1 KHA 1234"
            };

            await authService.RegisterAsync(registerDto);

            // Act 2: Verify Email OTP
            var authResult = await authService.VerifyEmailOtpAsync(new VerifyOtpDto
            {
                Email = "test@user.com",
                OtpCode = "123456"
            });

            Assert.Equal("mock_token", authResult.Token);

            var user = await dbContext.Users.FirstAsync(u => u.Email == "test@user.com");

            // Act 3: Get Profile
            var profile = await customerService.GetProfileAsync(user.Id);
            Assert.Equal("Test User", profile.FullName);
            Assert.Single(profile.Vehicles);
            Assert.Equal("BA 1 KHA 1234", profile.Vehicles[0].NumberPlate);

            // Act 4: Update Profile
            await customerService.UpdateProfileAsync(user.Id, new UpdateProfileDto { City = "Pokhara" });
            profile = await customerService.GetProfileAsync(user.Id);
            Assert.Equal("Pokhara", profile.City);

            // Act 5: Add Vehicle
            await customerService.AddVehicleAsync(user.Id, new CreateVehicleDto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2020,
                FuelType = "Petrol",
                NumberPlate = "BA 2 KHA 5678"
            });
            profile = await customerService.GetProfileAsync(user.Id);
            Assert.Equal(2, profile.Vehicles.Count);
        }

        [Fact]
        public async Task Feature10_Staff_Can_Search_Customers()
        {
            var dbContext = GetDbContext();
            
            // Seed
            var customer1 = new User { Id = Guid.NewGuid(), FullName = "Alice Doe", Email = "alice@test.com", PhoneNumber = "9812345678", Role = RoleType.Customer, PasswordHash = "hash" };
            var vehicle1 = new Vehicle { Id = Guid.NewGuid(), CustomerId = customer1.Id, Make = "Honda", Model = "City", NumberPlate = "XYZ-123", FuelType = FuelType.Petrol };
            
            dbContext.Users.Add(customer1);
            dbContext.Vehicles.Add(vehicle1);
            await dbContext.SaveChangesAsync();

            var customerService = new CustomerService(dbContext);

            // Search by Name
            var result1 = await customerService.SearchCustomersAsync("Alice", "name");
            Assert.Single(result1);

            // Search by Phone
            var result2 = await customerService.SearchCustomersAsync("981234", "phone");
            Assert.Single(result2);

            // Search by Vehicle
            var result3 = await customerService.SearchCustomersAsync("XYZ", "vehicle");
            Assert.Single(result3);
        }

        [Fact]
        public async Task Feature9_Staff_Can_Generate_Reports()
        {
            var dbContext = GetDbContext();
            var customerId = Guid.NewGuid();
            var customer = new User { Id = customerId, FullName = "Bob Smith", Email = "bob@test.com", Role = RoleType.Customer, PasswordHash="hash", PhoneNumber="123" };
            
            var invoice1 = new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, TotalAmount = 500, PaymentStatus = PaymentStatus.Paid, InvoiceNumber="INV-001" };
            var invoice2 = new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, TotalAmount = 600, PaymentStatus = PaymentStatus.Paid, InvoiceNumber="INV-002" };
            var invoice3 = new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, TotalAmount = 100, PaymentStatus = PaymentStatus.Unpaid, InvoiceNumber="INV-003" };

            dbContext.Users.Add(customer);
            dbContext.SalesInvoices.AddRange(invoice1, invoice2, invoice3);
            await dbContext.SaveChangesAsync();

            var reportService = new ReportService(dbContext);

            // 1. Get Regulars (Minimum 3 invoices)
            var regulars = await reportService.GetRegularCustomersAsync(3);
            Assert.Single(regulars);

            // 2. High Spenders (> 1000)
            var highSpenders = await reportService.GetHighSpendersAsync(1000);
            Assert.Single(highSpenders);
            Assert.Equal(1100, highSpenders[0].TotalSpent); // Only Paid is calculated

            // 3. Pending Credits
            var pending = await reportService.GetPendingCreditsAsync();
            Assert.Single(pending);
            Assert.Equal(100, pending[0].TotalPending);
        }

        [Fact]
        public async Task Feature11_Staff_Can_Send_Invoice()
        {
            var dbContext = GetDbContext();
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(s => s.SendInvoiceEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            var customer = new User { Id = Guid.NewGuid(), FullName = "Charlie", Email = "charlie@test.com", PasswordHash="hash", Role = RoleType.Customer };
            var invoiceId = Guid.NewGuid();
            var invoice = new SalesInvoice { Id = invoiceId, CustomerId = customer.Id, TotalAmount = 1000, InvoiceNumber = "INV-005" };
            
            dbContext.Users.Add(customer);
            dbContext.SalesInvoices.Add(invoice);
            await dbContext.SaveChangesAsync();

            var invoiceService = new SalesInvoiceService(dbContext, mockEmailService.Object);
            
            var result = await invoiceService.SendInvoiceEmailAsync(invoiceId);
            
            Assert.True(result);
            mockEmailService.Verify(s => s.SendInvoiceEmailAsync("charlie@test.com", "Charlie", "INV-005", It.IsAny<byte[]>()), Times.Once);
        }
    }
}
