namespace TechLibrary.Communication.Responses;
public class ResponseBooksJson
{
    public ResponsePaginationJson Pagination { get; set; } = default!; //é feito desta forma quando é uma classe 

    public List<ResponseBookJson> Books { get; set; } = [];

}
