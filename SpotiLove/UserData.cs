using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotiLove;

public class UserData
{
    public static UserData? Current { get; set; }

    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public static UserDto? CurrentDTO { get; set; }
    public UserDto ToDto()
    {
        return new UserDto
        {
            Id = this.Id,
            Name = this.Name,
        };

    }
}

