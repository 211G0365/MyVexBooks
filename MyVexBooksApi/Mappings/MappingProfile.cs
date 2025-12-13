using AutoMapper;
using MyVexBooks.Models.Entities;
using MyVexBooks.Models.DTOs;
namespace MyVexBooks.Mappings
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {

            // Usuario

            CreateMap(typeof(RegistroDTO), typeof(Usuarios));
            CreateMap(typeof(Usuarios), typeof(UsuarioDTO));

            
            // Libros
    
            CreateMap(typeof(Libros), typeof(LibroDTO));
            CreateMap(typeof(Libros), typeof(LibroCompletoDTO));


            //Partes
            CreateMap<Partes, ParteDTO>()
                .ForMember(dest => dest.Likes, opt => opt.MapFrom(src => src.Likes ?? 0));


        }
    }
}
