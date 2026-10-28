using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Services;

internal static class QueryShapes
{
    public static IQueryable<Order> TrackedOrders(AppDbContext db, int skip, int take) =>
        db.Orders
            .Include(order => order.Customer)
            .OrderBy(order => order.Id)
            .Skip(skip)
            .Take(take);

    public static IQueryable<Order> UntrackedOrders(AppDbContext db, int skip, int take) =>
        db.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .OrderBy(order => order.Id)
            .Skip(skip)
            .Take(take);

    public static IQueryable<Order> NPlusOneOrderPage(AppDbContext db, int skip, int take) =>
        db.Orders
            .AsNoTracking()
            .OrderBy(order => order.Id)
            .Skip(skip)
            .Take(take);

    public static IQueryable<NPlusOneOrderResponse> NPlusOneProjected(AppDbContext db, int skip, int take) =>
        db.Orders
            .AsNoTracking()
            .OrderBy(order => order.Id)
            .Skip(skip)
            .Take(take)
            .Select(order => new NPlusOneOrderResponse(
                order.Id,
                order.OrderNumber,
                order.Customer.Name,
                order.Items.Count,
                order.TotalAmount));

    public static IQueryable<Order> MaterializationSource(AppDbContext db) =>
        db.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .OrderBy(order => order.Id)
            .Take(QueryLimits.MaterializationSafetyCap);

    public static IQueryable<FilteredOrderRow> MaterializationComposed(
        AppDbContext db,
        OrderFilter filter,
        int skip,
        int take) =>
        db.Orders
            .AsNoTracking()
            .Where(order => order.Status == filter.Status && order.PlacedAt >= filter.PlacedAfter)
            .OrderBy(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Skip(skip)
            .Take(take)
            .Select(order => new FilteredOrderRow(
                order.Id,
                order.OrderNumber,
                order.Customer.Name,
                order.Status,
                order.PlacedAt,
                order.TotalAmount));

    public static IQueryable<Order> ProjectionEntities(AppDbContext db, int skip, int take) =>
        db.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .OrderBy(order => order.Id)
            .Skip(skip)
            .Take(take);

    public static IQueryable<ProjectedOrderResponse> ProjectionSelected(AppDbContext db, int skip, int take) =>
        db.Orders
            .AsNoTracking()
            .OrderBy(order => order.Id)
            .Skip(skip)
            .Take(take)
            .Select(order => new ProjectedOrderResponse(
                order.Id,
                order.OrderNumber,
                order.Customer.Name,
                order.PlacedAt,
                order.TotalAmount));

    public static IQueryable<Order> SqlInspectionSource(AppDbContext db) =>
        db.Orders
            .AsNoTracking()
            .OrderBy(order => order.Id)
            .Take(QueryLimits.MaterializationSafetyCap);

    public static IQueryable<SqlInspectionRow> SqlInspectionComposed(
        AppDbContext db,
        OrderFilter filter,
        int skip,
        int take) =>
        db.Orders
            .AsNoTracking()
            .Where(order => order.Status == filter.Status && order.PlacedAt >= filter.PlacedAfter)
            .OrderBy(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Skip(skip)
            .Take(take)
            .Select(order => new SqlInspectionRow(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PlacedAt,
                order.TotalAmount));
}
