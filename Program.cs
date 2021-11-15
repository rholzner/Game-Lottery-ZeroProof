// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Console.WriteLine("Press any key to continue...");	
Console.ReadKey();
//create game
var gameService = new GameService(new GameRepository(), new AccountLedgerRowRepo());
gameService.CreateGame("test game");
var allGames = gameService.GetGames();
foreach (var game in allGames)
{
	Console.WriteLine(game.name);
}
//set users
User user1 = new User(Guid.NewGuid(), "Kalle");
User user2 = new User(Guid.NewGuid(), "BatMan");
User user3 = new User(Guid.NewGuid(), "SupperMan");
User user4 = new User(Guid.NewGuid(), "SpiderMan");
User user5 = new User(Guid.NewGuid(), "Bert");

var userList = new List<User>()
{
	user1,user2,user3,user4,user5
};

//bye tickets
var gameInFocus = allGames.Single(x => x.name == "test game");
gameService.AddTickets(gameInFocus.Id,user1.Id,10,5);
gameService.AddTickets(gameInFocus.Id,user2.Id,15,5);
gameService.AddTickets(gameInFocus.Id,user3.Id,3,5);
gameService.AddTickets(gameInFocus.Id,user4.Id,14,5);
gameService.AddTickets(gameInFocus.Id,user5.Id,4,5);
gameService.AddTickets(gameInFocus.Id,user3.Id,3,5);

//list all games
allGames = gameService.GetGames();
foreach (var game in allGames)
{
	Console.WriteLine(game.name);
	Console.WriteLine("tickets: " + game.Tickets.Count);
}

//draw winners
var winner1 = gameService.GetWinnerTicket(gameInFocus.Id,1);
Console.WriteLine(userList.FirstOrDefault(x => x.Id == winner1.UserId).Name + " won ");
Console.ReadLine();
var winner2 = gameService.GetWinnerTicket(gameInFocus.Id,2);
Console.WriteLine(userList.FirstOrDefault(x => x.Id == winner2.UserId).Name + " won ");
Console.ReadLine();

var winner3 = gameService.GetWinnerTicket(gameInFocus.Id,3);
Console.WriteLine(userList.FirstOrDefault(x => x.Id == winner3.UserId).Name + " won ");
Console.ReadLine();

var winner4 = gameService.GetWinnerTicket(gameInFocus.Id,4);
Console.WriteLine(userList.FirstOrDefault(x => x.Id == winner4.UserId).Name + " won ");
Console.ReadLine();


//List saldo
var ListAllUsersBalance = gameService.GetAllUserAccountBalance();
foreach (var user in ListAllUsersBalance)
{
	var hasUser = userList.FirstOrDefault(x => x.Id == user.UserId);
	Console.WriteLine(hasUser.Name + " has " + user.Balance + " kr");
}

//set in money to user
gameService.AddMoneyToUser(user1.Id,-100);

Console.WriteLine();
Console.WriteLine("updated saldo");
Console.WriteLine();

//List saldo
ListAllUsersBalance = gameService.GetAllUserAccountBalance();
foreach (var user in ListAllUsersBalance)
{
	var hasUser = userList.FirstOrDefault(x => x.Id == user.UserId);
	Console.WriteLine(hasUser.Name + " has " + user.Balance + " kr");
}

Console.ReadLine();

public record class User(Guid Id, string Name);

public class GameService
{
	private readonly GameRepository _gameRepository;
	private readonly AccountLedgerRowRepo _accountLedgerRowRepo;
	public GameService(GameRepository gameRepository,AccountLedgerRowRepo accountLedgerRowRepo)
	{
		_accountLedgerRowRepo = accountLedgerRowRepo;
		_gameRepository = gameRepository;
	}

	public void CreateGame(Game game)
	{
		_gameRepository.Add(game);
	}
	public void CreateGame(string name)
	{
		_gameRepository.Add(new Game(0,Guid.NewGuid(),name, new List<Ticket>()));
	}

	public void CloseGame(int gameId)
	{
		var game = _gameRepository.Get(gameId);
		if (game.success)
		{
			game.data.ClosedAt = DateTime.UtcNow;
			_gameRepository.Update(game.data);
		}
	}
	
	public Game GetGame(int id)
	{
		return _gameRepository.Get(id).data;
	}
	public List<Game> GetGames()
	{
		return _gameRepository.GetAll();
	}
	
	public Game AddTickets(int gameId,Guid userId, int tickets,decimal price)
	{
		var game = _gameRepository.Get(gameId);
		if(game.success)
		{
			for (int i = 0; i < tickets; i++)
			{
				var ticket = new Ticket(0,Guid.NewGuid(),userId,DateTime.UtcNow,game.data.UniqId);
				game.data.Tickets.Add(ticket);
				_accountLedgerRowRepo.Add(new AccountLedgerAddRow(userId,game.data.UniqId,price),ticket);
			}
			_gameRepository.Update(game.data);
		}
		return game.data;
	}

	public Ticket GetWinnerTicket(int gameId,int winnerNr)
	{
		var game = _gameRepository.Get(gameId);
		if(game.success)
		{
			var winnerTicket = DrawWinnerTicket(game.data.Tickets);
			winnerTicket.isWinnerNr = winnerNr;
			_gameRepository.Update(game.data);
			return winnerTicket;
		}
		return null;
	}
	public Ticket DrawWinnerTicket(List<Ticket> tickets)
	{
		//TODO: make more true random
		var random = new Random();
		var ticketsInFocus = tickets.Where(t => t.isWinnerNr == 0).ToList();
		var winnerTicket = ticketsInFocus[random.Next(0,ticketsInFocus.Count)];
		return winnerTicket;
	}

	public IEnumerable<UserAccountBalance> GetAllUserAccountBalance()
	{
		return _accountLedgerRowRepo.ListAllUsersBalance();
	}

	public void AddMoneyToUser(Guid userId, decimal amount)
	{
		_accountLedgerRowRepo.Add(new AccountLedgerAddRow(userId,Guid.Empty,amount));
	}


}
public class GameRepository
{
	private readonly List<Game> _gameContext;
	public GameRepository()
	{
		_gameContext = new List<Game>();
	}
	public void Add(Game game)
	{
		_gameContext.Add(game);
	}
	public void Remove(Game game)
	{
		_gameContext.Remove(game);
	}
	public void Update(Game game)
	{
		_gameContext.Remove(game);
		_gameContext.Add(game);
	}
	public Result<Game> Get(int id)
	{
		var item = _gameContext.FirstOrDefault(x => x.Id == id); 
		return new Result<Game>(item,item is not null,"");
	}

	public List<Game> GetAll()
	{
		return _gameContext;
	}

}


public class AccountLedgerRowRepo
{
    private readonly List<AccountLedgerRow> _rows;
	public AccountLedgerRowRepo()
	{
		_rows = new List<AccountLedgerRow>();
	}
	public void Add(AccountLedgerAddRow row,Ticket ticket)
	{
		var balance = _rows.OrderByDescending(x => x.TimeStamp).FirstOrDefault(x => x.UserId == row.UserId)?.Balance ?? 0;
		balance += row.Amount;
		var hashChange = "TODO";
		_rows.Add(new AccountLedgerRow(0,Guid.NewGuid(),hashChange,DateTime.UtcNow,row.UserId,Guid.Empty,row.Amount,balance,"",ticket));
	}

	public void Add(AccountLedgerAddRow row)
	{
		var balance = _rows.OrderByDescending(x => x.TimeStamp).FirstOrDefault(x => x.UserId == row.UserId)?.Balance ?? 0;
		balance += row.Amount;
		var hashChange = "TODO";
		_rows.Add(new AccountLedgerRow(0,Guid.NewGuid(),hashChange,DateTime.UtcNow,row.UserId,Guid.Empty,row.Amount,balance,"",null));
	}
	
	public IEnumerable<UserAccountBalance> ListAllUsersBalance()
	{
		var result = new List<UserAccountBalance>();
		var userAccs = _rows.Select(x => x.UserId).Distinct().ToList();
		foreach (var userAcc in userAccs)
		{
			var balance = _rows.OrderByDescending(x => x.TimeStamp).FirstOrDefault(x => x.UserId == userAcc)?.Balance ?? 0;
			result.Add(new UserAccountBalance(userAcc,balance));
		}
		return result;
	}

}

public record UserAccountBalance(Guid UserId, decimal Balance);

public record class Result<Tdata>(Tdata data, bool success, string errorMessage);

public record class Game (
	int Id,
	Guid UniqId,
	string name,
	List<Ticket> Tickets)
	{
		public DateTime ClosedAt { get; set; }
	}

public record class Ticket(
	int id,
	Guid UniqId,
	Guid UserId,
	//string HasChange,
	DateTime TimeStamp,
	Guid GameId
	//int isWinnerNr
	)
	{
		public int isWinnerNr { get; set; }
	}

public record class AccountLedgerAddRow(
	Guid UserId,
	Guid GameId,
	decimal Amount);

public record class AccountLedgerRow(
	int id,
	//UniqId
	Guid UniqId,
	//Value to verfy that data has not bin editeds
	string HashChange,
	//Row was created
	DateTime TimeStamp,
	//User in question, aka account
	Guid UserId,
	//Who added the data, will propbebly be the same as userId
	Guid AddedBy,
	//Incoming update
	decimal Input,
	//New balance after addeding input
	decimal Balance,
	//Free text comment if needed
	string Comment,
	//Is it connected to tickets
	Ticket? GameRow);