﻿using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Innofactor.EfCoreJsonValueConverter.Test.Components;
using Innofactor.EfCoreJsonValueConverter.Test.Entities;

namespace Innofactor.EfCoreJsonValueConverter.Test {

  /// <summary>
  /// Tests for <see cref="ModelBuilderExtensions"/>.
  /// </summary>
  [TestClass]
  public class ModelBuilderExtensionsTests {

    private readonly ModelBuilder _modelBuilder;

    public ModelBuilderExtensionsTests() {
      _modelBuilder = new ModelBuilder(TestHelpers.GetDefaultConventions());
    }

    [TestMethod]
    public void AddJsonFields() {

      _modelBuilder.Entity<Customer>().Property(m => m.Address);
      _modelBuilder.AddJsonFields();        

      var model = _modelBuilder.Model;
      var modelType = model.FindEntityType(typeof(Customer));
      var modelProperty = modelType.FindProperty(nameof(Customer.Address));

      Assert.IsInstanceOfType(modelProperty.GetValueConverter(), typeof(JsonValueConverter<Address>), "Value converter was applied");
    }

    [TestMethod]
    public void AddJsonFields_NavigationProperty() {

      _modelBuilder.Entity<Customer>();
      _modelBuilder.AddJsonFields();

      var model = _modelBuilder.Model;
      var modelType = model.FindEntityType(typeof(Customer));
      var modelProperty = modelType.FindProperty(nameof(Customer.Address));

      Assert.IsNotNull(modelProperty, "Navigation property was handled as entity property");
      Assert.IsInstanceOfType(modelProperty.GetValueConverter(), typeof(JsonValueConverter<Address>), "Value converter was applied");
    }

    [TestMethod]
    public void AddJsonFields_ShadowProperty() {

      _modelBuilder.Entity<CustomerWithPlainField>().Property(p => p.Name).HasField("_name");
      _modelBuilder.AddJsonFields();        

      var model = _modelBuilder.Model;
      var modelType = model.FindEntityType(typeof(CustomerWithPlainField));
      var modelProperty = modelType.FindProperty("Name");

      Assert.IsNotNull(modelProperty, "Plain field was added");
    }

    [TestMethod]
    public void AddJsonFields_SkipConventions() {

      _modelBuilder.Entity<Customer>();
      _modelBuilder.AddJsonFields();

      var model = _modelBuilder.Model;
      var modelType = model.FindEntityType(typeof(Office));
      var modelProperty = modelType.FindProperty(nameof(Office.Address));

      Assert.IsNull(modelProperty, "Office not explicitly registered as entity, don't initialize it's properties by convention");
    }

    [TestMethod]
    public void AddJsonFields_IncludeConventions() {

      _modelBuilder.Entity<Customer>();
      _modelBuilder.AddJsonFields(skipConventionalEntities: false);

      var model = _modelBuilder.Model;
      var modelType = model.FindEntityType(typeof(Office));
      var modelProperty = modelType.FindProperty(nameof(Office.Address));

      Assert.IsNotNull(modelProperty, "Office was not explicitly registered as entity, go on and initialize properties by convention");
    }

    [TestMethod]
    public void AddJsonFields_IgnoredProperties() {

      _modelBuilder.Entity<Customer>().Ignore(c => c.Office);
      _modelBuilder.AddJsonFields();

      var model = _modelBuilder.Model;
      var modelType = model.FindEntityType(typeof(Customer));
      var ignoredByBuilder = modelType.FindProperty(nameof(Customer.Office));
      var ignoredByAnnotation = modelType.FindProperty(nameof(Customer.OfficeNotMapped));

      Assert.IsNull(ignoredByBuilder, "Property ignored via builder was not initialized");
      Assert.IsNull(ignoredByAnnotation, "Property ignored via attribute was not initialized");
    }

    [TestMethod]
    public void DetectChanges_PlainObject() {

      using (var db  = new TestDbContext()) {

        var customer = new Customer { Address = new Address { Street = "Street 1" }};
        db.Add(customer);
        db.SaveChanges();

        // Note: changing tracking does not work without reloading. This might be an issue with EF Core itself?
        db.Entry(customer).State = EntityState.Detached;
        customer = db.Customers.Find(customer.Id);

        Assert.AreEqual(EntityState.Unchanged, db.Entry(customer).State, "Precondition: entity is marked as unchanged");

        customer.Address.Street = "Street 2";

        db.ChangeTracker.DetectChanges();

        Assert.AreEqual(EntityState.Modified, db.Entry(customer).State, "Entity is marked as modified");
        Assert.IsTrue(db.Entry(customer).Property(m => m.Address).IsModified, "Property is marked as modified");
      }
    }

    [TestMethod]
    public void DetectChanges_WithCustomEquality() {

      using (var db  = new TestDbContext()) {

        var customer = new Customer { Address2 = new AddressWithEquality { Street = "Street 1" }};
        db.Add(customer);
        db.SaveChanges();

        db.Entry(customer).State = EntityState.Detached;
        customer = db.Customers.Find(customer.Id);

        Assert.AreEqual(EntityState.Unchanged, db.Entry(customer).State, "Precondition: entity is marked as unchanged");

        customer.Address2.Street = "Street 2";

        db.ChangeTracker.DetectChanges();

        Assert.AreEqual(EntityState.Modified, db.Entry(customer).State, "Entity is marked as modified");
        Assert.IsTrue(db.Entry(customer).Property(m => m.Address2).IsModified, "Property is marked as modified");
      }
    }

  }
}
