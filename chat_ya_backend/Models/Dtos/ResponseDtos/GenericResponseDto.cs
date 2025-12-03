using chat_ya_backend.Models.Dtos.ErrorDtos;
using Newtonsoft.Json;

namespace chat_ya_backend.Models.Dtos.ResponseDtos
{
    public class GenericResponseDto
    {
        #region Miembros privados

        private bool isValid;
        private GenericErrorDto error;

        #endregion

        #region Constructores

        /// <summary>
        ///     Constructor por defecto
        /// </summary>
        public GenericResponseDto()
        {
            isValid = true;
            error = new GenericErrorDto(); 
            // Inicializa la propiedad de tipo 'object' a un valor no nulo
            ReturnData = new object();
        }

        #endregion

        #region Propiedades

        /// <summary>
        ///     Propiedad que indica si han habido errores
        /// </summary>
        public bool IsValid { get { return isValid; } }

        public string? Id { get; set; }

        // Mapeamos la propiedad que usa el controlador
        public object Data
        {
            get => ReturnData;
            set => ReturnData = value;
        }

        /// <summary>
        ///     Error a devolver al usuario <see cref="GenericErrorDto"/>
        /// </summary>
        public GenericErrorDto Error
        {
            get { return error; }
            set
            {
                if (value != null)
                {
                    error = value;
                    isValid = false;
                }
            }
        }

        /// <summary>
        ///     Método encargado de determinar si se debe serializar o no la propiedad error
        /// </summary>
        /// <returns></returns>
        public bool ShouldSerializeError()
        {
            return !IsValid;
        }

        /// <summary>
        ///     Objeto de respuesta (Tu propiedad original)
        /// </summary>
        [JsonIgnore]
        public object ReturnData { get; set; } = new object(); 
                                                               // La inicialización en el constructor (línea 26) también es válida, 
                                                               // pero esta forma es más concisa. Dejo ambas para seguridad.
        #endregion
    }
}