namespace chat_ya_backend.Models.Dtos.ResponseDtos
{
    public class CreateEditRemoveResponseDto
    {
        public int Id { get; set; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }

        #region Constructors

        /// <summary>
        /// Asegura que la lista Errors nunca sea nula antes de que se use
        /// </summary>
        public CreateEditRemoveResponseDto() 
        {
            Errors = new List<string>();
        }

        #endregion

        public void IsSuccess(int id)
        {
            Id = id;
            Success = true;
        }
    }
}
