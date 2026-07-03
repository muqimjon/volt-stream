namespace VoltStream.WPF.Sales.Mappers;

using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Mapster;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Sales.ViewModels;

public class SaleMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SaleResponse, SaleViewModel>()
            .Map(dest => dest.Date, src => src.Date.LocalDateTime);

        config.NewConfig<SaleItemResponse, SaleItemViewModel>();
        config.NewConfig<SaleItemViewModel, SaleItemRequest>();

        config.NewConfig<WarehouseStockResponse, WarehouseStockViewModel>();
    }
}
