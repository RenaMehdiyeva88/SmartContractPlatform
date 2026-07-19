using AutoMapper;
using SmartContractPlatform.BLL.DTOs;
using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.BLL.Mapping;

public class SmartContractProfile : Profile
{
    public SmartContractProfile()
    {
        CreateMap<Template, TemplateDto>();
        CreateMap<AuditLog, AuditLogDto>();
        CreateMap<Document, DocumentDto>();
        CreateMap<ContractParty, ContractPartyDto>();
        CreateMap<Contract, ContractDto>()
            .ForMember(destination => destination.TemplateName, options => options.MapFrom(source => source.Template == null ? null : source.Template.Name));
    }
}
