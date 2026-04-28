using System.ComponentModel.DataAnnotations;

namespace APBD_07.DTOs;

public class ErrorResponseDTO
{
    [MaxLength(256)]
    public string Message { get; set; } = string.Empty;
}