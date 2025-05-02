using System;
using AutoMapper;

namespace Api.Features.DocumentIntelligence;

public class BenefitExportMappingProfile : Profile
{
    public BenefitExportMappingProfile()
    {
        CreateMap<ExtractedBenefitResponse, BenefitsForExcelExport>()
        .ForMember(dest => dest.Benefit, opt => opt.MapFrom(src => src.Benefit))
        .ForMember(dest => dest.InNetworkConditions, opt => opt.MapFrom(src => src.InNetworkConditions))
        .ForMember(dest => dest.OutOfNetworkConditions, opt => opt.MapFrom(src => src.OutOfNetworkConditions))
        .ForMember(dest => dest.Limitations, opt => opt.MapFrom(src => src.Limitations));
    }

}

public class TestCasesToExportProfile : Profile
{
    public TestCasesToExportProfile()
    {
        CreateMap<GeneratedTestCase, TestCasesToExport>()
        .ForMember(d => d.IcdCodes,
                       o => o.MapFrom(s => s.IcdCodes.Length > 0
                                         ? string.Join(",", s.IcdCodes)
                                         : string.Empty))
        .ForMember(d => d.ProcedureCodes,
                    o => o.MapFrom(s => s.ProcedureCodes.Length > 0
                                        ? string.Join(",", s.ProcedureCodes)
                                        : string.Empty))
        .ForMember(d => d.PlaceOfService,
                    o => o.MapFrom(s => s.PlaceOfService.Length > 0
                                        ? string.Join(",", s.PlaceOfService)
                                        : string.Empty))
        .ForMember(d => d.Benefit, opt => opt.Ignore())
        ;

    }
}
