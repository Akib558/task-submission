/*
 
Case Stude: 01 
   
Background: You need to build a rest API where User can login and logout, 
User can upload a csv file with keyword. With each keyword software should 
perform a search to google with a network call. Result from the network call 
should be stored in database
      
Question: What are the classes you might need in this software solution?

Controller:

- AuthController
- FileController

Service:

- AuthService
- UserService
- SearchService
- FileUploadService
- KeyWordParserService
- ResultStoreService

Repository:

- FIleStorageRepository


DTOs:

- LoginRequestDto / LoginResponseDto
- UserInfoDto
- FileInfoDto
- KeyWordDTO
- SearchResultDto



*/

public class LoginRequestDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; }
}

public class RegisterRequestDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class FileInfoDto
{
    public Guid FileId { get; set; }
    public string FileName { get; set; }
}

public class KeyWordDTO
{
    public string Keyword { get; set; }
}

public class SearchResultDto
{
    public string KeyWord { get; set; }
    public string Result { get; set; }
}


public class AuthController
{
    private readonly AuthService _authService;
    public AuthController(AuthService authService) => _authService = authService;

    public LoginResponseDto Login(LoginRequestDto loginRequest)
    {
        User user = _authService.Login(loginRequest);
        return new LoginResponseDto() { Token = user.Token };
    }
    public string Register(RegisterRequestDto registerRequest) => _authService.Register(registerRequest);
}

public class FileController
{
    private readonly FileUploadService _fileUploadService;
    public FileController(FileUploadService fileUploadService) => _fileUploadService = fileUploadService;

    public void Upload(byte[] file) => _fileUploadService.Upload(file);
}

public class AuthService
{
    private readonly AuthRepository _authRepositoy;
    public AuthService(AuthRepository authRepositoy) => _authRepositoy = authRepositoy;
    
    public User Login(LoginRequestDto request) => _authRepositoy.GetCredentials(request.Email, request.Password);
    public string Register(RegisterRequestDto request) => _authRepositoy.Register(request);
}

public class FileUploadService
{
    private readonly KeywordParserService _parser;
    private readonly SearchService _searchService;
    private readonly ResultStoreService _storeService;

    public FileUploadService(KeywordParserService parser, SearchService searchService, ResultStoreService storeService)
    {
        _parser = parser;
        _searchService = searchService;
        _storeService = storeService;
    }

    public void Upload(byte[] file)
    {
        string[] keyWordList = _parser.Parse(file);
        for(string keyWord in keyWordList)
        {
            string searchResult = _searchService.GetSearchResult(keyWord);
            _storeService.Store(keyWord, searchResult);
        }
    }
}

public class KeywordParserService
{
    public List<string> Parse(byte[] file)
    {
        return new List<string>();
    }
}

public class SearchService
{
    private readonly GoogleSearchClient _client;
    public SearchService(GoogleSearchClient client) => _client = client;

    public string GetSearchResult(string word)
    {
       return _client.Search(word);
    }
}

public class GoogleSearchClient
{
    public string Search(string word)
    {
        return "Result";
    }
}

public class ResultStoreService
{
    private readonly ResultStoreRepository _repository;
    
    public ResultStoreService(ResultStoreRepository repository) => _repository = repository;

    public void Store(string keyWord, string searchResult)
    {
        _repository.Store(keyWord, searchResult);
    }
}


public class User
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Token { get; set; }
}

public class AuthRepository
{
    public User GetCredentials(string email, string password)
    {
        return new User();
    }
    
    public string Add(RegisterRequestDto request)
    {
        return "User registered successfully";
    }
}

public class ResultStoreRepository
{
    public void Store(string keyWord, string searchResult)
    {
        // storing in database
    }
}