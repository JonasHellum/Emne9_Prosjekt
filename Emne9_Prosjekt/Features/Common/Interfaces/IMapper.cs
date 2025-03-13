namespace Emne9_Prosjekt.Features.Common.Interfaces;

public interface IMapper<TModel, TDto>
{
    TDto MapToDTO(TModel model);
    TModel MapToModel(TDto dto);
}