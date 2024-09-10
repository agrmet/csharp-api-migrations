﻿using exercise.pizzashopapi.DTO;
using exercise.pizzashopapi.Models;
using exercise.pizzashopapi.Repository;
using exercise.pizzashopapi.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace exercise.pizzashopapi.EndPoints
{
    public static class PizzaShopApi
    {
        public static void ConfigurePizzaShopApi(this WebApplication app)
        {
            var pizzashop = app.MapGroup("pizzashop");
            pizzashop.MapGet("/pizzas/", GetPizzas);
            pizzashop.MapGet("/pizzas/{id}", GetAPizza);
            pizzashop.MapGet("/customers/", GetCustomers);
            pizzashop.MapGet("/customers/{id}", GetACustomer);
            pizzashop.MapGet("/orders/", GetOrders);
            pizzashop.MapGet("/orders/{id}", GetOrdersByCustomer);
            pizzashop.MapPost("/pizzas", CreatePizza);
            pizzashop.MapPost("/customers", CreateCustomer);
            pizzashop.MapPost("/orders", CreateOrder);
            pizzashop.MapDelete("/pizzas", DeletePizza);
            pizzashop.MapDelete("/customers", DeleteCustomer);
            pizzashop.MapDelete("/orders", DeleteOrder);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public static async Task<IResult> GetPizzas(IRepository repository)
        {
            var pizzas = await repository.GetPizzas();
            if (!pizzas.Any()) return TypedResults.NotFound("No pizzas found");

            return TypedResults.Ok(pizzas);

        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> GetAPizza(IRepository repository, int id)
        {
            var pizza = await repository.GetPizza(id);
            if (pizza == null) return TypedResults.NotFound($"A pizza with id {id} was not found");

            return TypedResults.Ok(pizza);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> GetCustomers(IRepository repository)
        {
            var customers = await repository.GetCustomers();
            if (!customers.Any()) return TypedResults.NotFound("No customers found");

            return TypedResults.Ok(customers);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> GetACustomer(IRepository repository, int id)
        {
            var customer = await repository.GetCustomer(id);
            if (customer == null) return TypedResults.NotFound($"Customer with id {id} was not found");

            return TypedResults.Ok(customer);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> GetOrders(IRepository repository)
        {
            var orders = await repository.GetAllOrders();
            if (!orders.Any()) return TypedResults.NotFound("No orders were found");

            List<OrderDTO> ordersDTO = (from order in orders select order.ToOrderDTO()).ToList();

            return TypedResults.Ok(ordersDTO);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> GetOrdersByCustomer(IRepository repository, int id)
        {
            var customer = await repository.GetCustomer(id);
            if (customer is null) return TypedResults.NotFound("The customer was not found");

            var orders = await repository.GetOrdersByCustomer(id);
            if (!orders.Any()) return TypedResults.NotFound("This customer does not have any orders");

            List<OrderDTO> ordersDTO = (from order in orders select order.ToOrderDTO()).ToList();

            return TypedResults.Ok(ordersDTO);
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public static async Task<IResult> CreatePizza(IRepository repository, PizzaPostModel pizzaPost)
        {
            if (pizzaPost.Name.Length == 0) return TypedResults.BadRequest("A pizzas name cannot be an empty string");
            var pizzaCollision = await repository.GetPizza(pizzaPost.Name);
            if (pizzaCollision is not null) return TypedResults.BadRequest("A pizza with that name already exists");

            Pizza pizza = new() { Name = pizzaPost.Name, Price = pizzaPost.Price };

            await repository.CreatePizza(pizza);

            return TypedResults.Created("https://localhost:7138/pizzashop/pizzas/id", pizza.Id);
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public static async Task<IResult> CreateCustomer(IRepository repository, CustomerPostModel customerPost)
        {
            if (customerPost.Name.Length < 2) return TypedResults.BadRequest("A customers name cannot be an empty string or less than 2 characters");

            Customer customer = new() { Name = customerPost.Name };

            await repository.CreateCustomer(customer);

            return TypedResults.Created("https://localhost:7138/pizzashop/customers/id", customer.Id);
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> CreateOrder(IRepository repository, OrderPostModel orderPost)
        {
            var customer = await repository.GetCustomer(orderPost.Customer);
            if (customer is null) return TypedResults.NotFound("The customer was not found");

            var pizza = await repository.GetPizza(orderPost.Pizza);
            if (pizza is null) return TypedResults.NotFound("The pizza was not found");

            Order order = new Order() { OrderDate = DateTime.UtcNow, Customer = customer, CustomerId = customer.Id, Pizza = pizza, PizzaId = pizza.Id };
            await repository.CreateOrder(order);

            return TypedResults.Created("https://localhost:7138/pizzashop/orders/id", (order.Customer, order.Pizza));
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> DeletePizza(IRepository repository, int id)
        {
            var result = await repository.DeletePizza(id);
            if (result is null) return TypedResults.NotFound("Pizza was not found");

            return TypedResults.Ok(result);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> DeleteCustomer(IRepository repository, int id)
        {
            var result = await repository.DeleteCustomer(id);
            if (result is null) return TypedResults.NotFound("Customer was not found");

            return TypedResults.Ok(result);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public static async Task<IResult> DeleteOrder(IRepository repository, int customer, int pizza)
        {
            var result = await repository.DeleteOrder(customer, pizza);

            if (result is null) return TypedResults.NotFound("Order was not found. Check customer and pizza id's");
            return TypedResults.Ok(result.ToOrderDTO());
        }

        private static OrderDTO ToOrderDTO(this Order order)
        {
            return new OrderDTO() {OrderDate = order.OrderDate, Customer = order.Customer.Name, Pizza = order.Pizza.Name};
        }
    }
}
