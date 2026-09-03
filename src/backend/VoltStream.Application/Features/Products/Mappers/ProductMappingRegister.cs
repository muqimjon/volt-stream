namespace VoltStream.Application.Features.Products.Mappers;

using Mapster;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Products.Commands;
using VoltStream.Application.Features.Products.DTOs;
using VoltStream.Domain.Entities;

public class ProductMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProductCommand, Product>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<UpdateProductCommand, Product>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<Product, ProductDto>();
        config.NewConfig<Product, ProductForWarehouseDto>();
        config.NewConfig<Product, ProductForCategoryDto>();
    }
}
