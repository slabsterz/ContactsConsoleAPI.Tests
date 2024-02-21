using ContactsConsoleAPI.Business;
using ContactsConsoleAPI.Business.Contracts;
using ContactsConsoleAPI.Data.Models;
using ContactsConsoleAPI.DataAccess;
using ContactsConsoleAPI.DataAccess.Contrackts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsConsoleAPI.IntegrationTests.NUnit
{
    public class IntegrationTests
    {
        private TestContactDbContext dbContext;
        private IContactManager contactManager;

        [SetUp]
        public void SetUp()
        {
            this.dbContext = new TestContactDbContext();
            this.contactManager = new ContactManager(new ContactRepository(this.dbContext));
        }


        [TearDown]
        public void TearDown()
        {
            this.dbContext.Database.EnsureDeleted();
            this.dbContext.Dispose();
        }


        //positive test
        [Test]
        public async Task AddContactAsync_ShouldAddNewContact()
        {
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            await contactManager.AddAsync(newContact);

            var dbContact = await dbContext.Contacts.FirstOrDefaultAsync(c => c.Contact_ULID == newContact.Contact_ULID);

            Assert.NotNull(dbContact);
            Assert.AreEqual(newContact.FirstName, dbContact.FirstName);
            Assert.AreEqual(newContact.LastName, dbContact.LastName);
            Assert.AreEqual(newContact.Phone, dbContact.Phone);
            Assert.AreEqual(newContact.Email, dbContact.Email);
            Assert.AreEqual(newContact.Address, dbContact.Address);
            Assert.AreEqual(newContact.Contact_ULID, dbContact.Contact_ULID);
        }

        //Negative test
        [Test]
        public async Task AddContactAsync_TryToAddContactWithInvalidCredentials_ShouldThrowException()
        {
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "invalid_Mail", //invalid email
                Gender = "Male",
                Phone = "0889933779"
            };

            var ex = Assert.ThrowsAsync<ValidationException>(async () => await contactManager.AddAsync(newContact));
            var actual = await dbContext.Contacts.FirstOrDefaultAsync(c => c.Contact_ULID == newContact.Contact_ULID);

            Assert.IsNull(actual);
            Assert.That(ex?.Message, Is.EqualTo("Invalid contact!"));

        }

        [Test]
        public async Task DeleteContactAsync_WithValidULID_ShouldRemoveContactFromDb()
        {
            // Arrange
            var contact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Gender = "Male",
                Contact_ULID = "1234ABCD12"
            };

            await contactManager.AddAsync(contact);

            // Act
            await contactManager.DeleteAsync(contact.Contact_ULID);
            var contactInDb = dbContext.Contacts.FirstOrDefault(x => x.Contact_ULID == contact.Contact_ULID);

            // Assert
            Assert.IsNull(contactInDb);
        }

        [Test]
        public async Task DeleteContactAsync_TryToDeleteWithNullOrWhiteSpaceULID_ShouldThrowException()
        {
            // Arrange
            var contact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Gender = "Male",
                Contact_ULID = "1234ABCD12"
            };

            await contactManager.AddAsync(contact);
            string expectedErrorMessage = "ULID cannot be empty.";

            // Act & Assert
            var result = Assert.ThrowsAsync<ArgumentException>(async () => await contactManager.DeleteAsync(null));
            Assert.That(result.Message, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public async Task GetAllAsync_WhenContactsExist_ShouldReturnAllContacts()
        {
            // Arrange
            var firstContact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Gender = "Male",
                Contact_ULID = "1234ABCD12"
            };

            var secondContact = new Contact()
            {
                Id = 12,
                FirstName = "Vladko",
                LastName = "Vladkov",
                Address = "Sky Str.",
                Phone = "0888123123",
                Email = "vladko@email.com",
                Gender = "Male",
                Contact_ULID = "9999UIOP12"
            };

            await contactManager.AddAsync(firstContact);
            await contactManager.AddAsync(secondContact);

            // Act
            var result = await contactManager.GetAllAsync();
            var firstContactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == firstContact.Id);
            var secondContactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == secondContact.Id);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(firstContactInDb.Id, Is.EqualTo(firstContact.Id));
            Assert.That(secondContactInDb.Id, Is.EqualTo(secondContact.Id));
        }

        [Test]
        public async Task GetAllAsync_WhenNoContactsExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string expectedErrorMessage = "No contact found.";

            // Act & Assert
            var result = Assert.ThrowsAsync<KeyNotFoundException>(async () => await contactManager.GetAllAsync());
            Assert.That(result.Message, Is.EqualTo(expectedErrorMessage));
          
        }

        [Test]
        public async Task SearchByFirstNameAsync_WithExistingFirstName_ShouldReturnMatchingContacts()
        {
            // Arrange
            var firstContact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Gender = "Male",
                Contact_ULID = "1234ABCD12"
            };

            var secondContact = new Contact()
            {
                Id = 12,
                FirstName = "Peter",
                LastName = "Vladkov",
                Address = "Sky Str.",
                Phone = "0888123123",
                Email = "vladko@email.com",
                Gender = "Male",
                Contact_ULID = "9999UIOP12"
            };

            await contactManager.AddAsync(firstContact);
            await contactManager.AddAsync(secondContact);

            // Act
            var result = await contactManager.SearchByFirstNameAsync(firstContact.FirstName);
            var firstContactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == firstContact.Id);
            var secondContactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == secondContact.Id);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(firstContactInDb.FirstName, Is.EqualTo(secondContactInDb.FirstName));
            Assert.That(firstContactInDb.Id, Is.Not.EqualTo(secondContactInDb.Id));

        }

        [Test]
        public async Task SearchByFirstNameAsync_WithNonExistingFirstName_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string nonExistingFirstName = "Random";
            string expectedErrorMessage = "No contact found with the given first name.";

            // Act & Assert
            var result = Assert.ThrowsAsync<KeyNotFoundException>( async () => await contactManager.SearchByFirstNameAsync(nonExistingFirstName));
            Assert.That(result.Message, Is.EqualTo(expectedErrorMessage));
        }
        
        [Test]
        public async Task SearchByFirstNameAsync_WithNullFirstName_ShouldThrowArgumentException()
        {
            // Arrange
            string nullFirstName = null;
            string expectedErrorMessage = "First name cannot be empty.";

            // Act & Assert
            var result = Assert.ThrowsAsync<ArgumentException>(async () => await contactManager.SearchByFirstNameAsync(nullFirstName));
            Assert.That(result.Message, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public async Task SearchByLastNameAsync_WithExistingLastName_ShouldReturnMatchingContacts()
        {
            // Arrange
            var firstContact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Gender = "Male",
                Contact_ULID = "1234ABCD12"
            };

            var secondContact = new Contact()
            {
                Id = 12,
                FirstName = "Vladko",
                LastName = "Petrov",
                Address = "Sky Str.",
                Phone = "0888123123",
                Email = "vladko@email.com",
                Gender = "Male",
                Contact_ULID = "9999UIOP12"
            };

            await contactManager.AddAsync(firstContact);
            await contactManager.AddAsync(secondContact);

            // Act
            var result = await contactManager.SearchByLastNameAsync(firstContact.LastName);
            var firstContactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == firstContact.Id);
            var secondContactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == secondContact.Id);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(firstContactInDb.LastName, Is.EqualTo(secondContactInDb.LastName));
            Assert.That(firstContactInDb.Id, Is.Not.EqualTo(secondContactInDb.Id));
            
        }

        [Test]
        public async Task SearchByLastNameAsync_WithNonExistingLastName_ShouldThrowKeyNotFoundException()
        {
            /// Arrange
            string nonExistingLastName = "Random";
            string expectedErrorMessage = "No contact found with the given last name.";

            // Act & Assert
            var result = Assert.ThrowsAsync<KeyNotFoundException>(async () => await contactManager.SearchByLastNameAsync(nonExistingLastName));
            Assert.That(result.Message, Is.EqualTo(expectedErrorMessage));
        }
        
        [Test]
        public async Task SearchByLastNameAsync_WithNullLastName_ShouldThrowArgumentException()
        {
            // Arrange
            string nullLasttName = null;
            string expectedErrorMessage = "Last name cannot be empty.";

            // Act & Assert
            var result = Assert.ThrowsAsync<ArgumentException>(async () => await contactManager.SearchByLastNameAsync(nullLasttName));
            Assert.That(result.Message, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public async Task SearchByLastNameAsync_WithLongLastName_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string longLasttName = new string('a', 101);
            string expectedErrorMessage = "No contact found with the given last name.";

            // Act & Assert
            var result = Assert.ThrowsAsync<KeyNotFoundException>(async () => await contactManager.SearchByLastNameAsync(longLasttName));
            Assert.That(result.Message, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public async Task GetSpecificAsync_WithValidULID_ShouldReturnContact()
        {
            // Arrange
            var firstContact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Gender = "Male",
                Contact_ULID = "1234ABCD12"
            };

            await contactManager.AddAsync(firstContact);

            // Act
            var result = await contactManager.GetSpecificAsync(firstContact.Contact_ULID);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Contact_ULID, Is.EqualTo(firstContact.Contact_ULID));
        }

        [Test]
        public async Task GetSpecificAsync_WithMissingULID_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string ulid = "123123OOOOPO";
            string exceptionMessage = $"No contact found with ULID: {ulid}";

            // Act & Assert
            var result = Assert.ThrowsAsync<KeyNotFoundException>( async () => await contactManager.GetSpecificAsync(ulid));
            Assert.That(result.Message, Is.EqualTo(exceptionMessage));         
        }
        
        [Test]
        public async Task GetSpecificAsync_WithEmptyULID_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string ulid = string.Empty;
            string exceptionMessage = "ULID cannot be empty.";

            // Act & Assert
            var result = Assert.ThrowsAsync<ArgumentException>(async () => await contactManager.GetSpecificAsync(ulid));
            Assert.That(result.Message, Is.EqualTo(exceptionMessage));
        }


        [Test]
        public async Task UpdateAsync_WithValidContact_ShouldUpdateContact()
        {
            // Arrange
            var firstContact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Gender = "Male",
                Contact_ULID = "1234ABCD12"
            };

            await contactManager.AddAsync(firstContact);

            string newGenger = "Female";

            firstContact.Gender = newGenger;

            // Act
            await contactManager.UpdateAsync(firstContact);
            var contactInDb = await dbContext.Contacts.FirstOrDefaultAsync( x => x.Id == firstContact.Id);

            // Assert
            Assert.That(contactInDb.Gender, Is.EqualTo(newGenger));
            
        }

        [Test]
        public async Task UpdateAsync_WithInvalidContact_ShouldThrowValidationException()
        {
            // Arrange
            var firstContact = new Contact()
            {
                Id = 52,
                FirstName = "Peter",
                LastName = "Petrov",
                Address = "Crimson Str.",
                Phone = "0999123123",
                Email = "test@email.com",
                Contact_ULID = "1234ABCD12"
            };

            // Act & Assert
            var result = Assert.ThrowsAsync<ValidationException>( async () => await contactManager.UpdateAsync(firstContact));
        }

        [Test]
        public async Task UpdateAsync_WithINullContact_ShouldThrowValidationException()
        {
            // Arrange
            Contact firstContact = null;

            // Act & Assert
            var result = Assert.ThrowsAsync<ValidationException>(async () => await contactManager.UpdateAsync(firstContact));
        }
    }
}
