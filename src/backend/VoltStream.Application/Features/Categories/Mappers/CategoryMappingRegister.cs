namespace VoltStream.Application.Features.Categories.Mappers;

using Mapster;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Categories.Commands;
using VoltStream.Application.Features.Categories.DTOs;
using VoltStream.Domain.Entities;

public class CategoryMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCategoryCommand, Category>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<UpdateCategoryCommand, Category>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<Category, CategoryDto>();
        config.NewConfig<Category, CategoryForProductDto>();
    }
}
