# csharp-forallp

C# IEnumerable<X>.ForAll on steroids, parallel and with progress.

ForAllP is an extension method to the `IEnumerable<T>` interface.

Similar to `ForAll`, it executes some code on each item of a collections:

- In parallel. Right now, the number of concurrent threads is managed by the runtime library.
- With progress handling. Five callbacks are provided:
	- Total start
	- Item start
	- Item progress
	- Item finished
	- Total finished

## Install

Right now, you have to copy and paste the code in [ForAllP.cs](ForAllP/ForAllP.cs) into your project.

## Usage

```c#
string[] data = new string[] { "012", "123", "234", "345", "456", "567", "678", "789", "890", "901" };

Random rnd = new Random();

data.ForAllP(
	body: (x, callback_percentage) => {
		callback_percentage(0);
		Thread.Sleep(rnd.Next(500, 1000));
		callback_percentage(25);
		Thread.Sleep(rnd.Next(500, 1000));
		callback_percentage(50);
		Thread.Sleep(rnd.Next(500, 1000));
		callback_percentage(75);
		Thread.Sleep(rnd.Next(500, 1000));
		callback_percentage(100);
	},
	item_progress: (x, n, t, p) => {
		Console.WriteLine(string.Format("Item progress: {0} ({1}/{2}): {3:P2}", x.ToString(), n, t, p / 100));
	},
	item_started: (x, n, t) => {
		Console.WriteLine(string.Format("Item started: {0} ({1}/{2})", x.ToString(), n, t));
	},
	item_finished: (x, n, t) => {
		Console.WriteLine(string.Format("Item finished: {0} ({1}/{2})", x.ToString(), n, t));
	},
	total_started: () => {
		Console.WriteLine("Start.");
	},
	total_finished: () => {
		Console.WriteLine("End.");
	}
);
```

For the sake of simplicity, it does not appear in the code above,
but please do not forget to add this two lines at the end of every progress handler:

```c#
Refresh();
Application.DoEvents();
```

and also this line at the beginning of the `total_finished` progress handler:

```c#
Application.DoEvents();
```

## Change log

Please see the [CHANGELOG](CHANGELOG.md) for more information what has changed recently.

## Testing

## Contributing

Please see [CONTRIBUTING](CONTRIBUTING.md) and [CONDUCT](CONDUCT.md) for details.

## Security

If you discover any security related issues, please email DM [@jotaelesalinas](http://twitter.com/jotaelesalinas) instead of using the issue tracker.

## To do

- [ ] Add some comments
- [ ] Add tests
- [ ] Create external nuget library
- [ ] Add option to make it async
- [ ] Add option to limit number of threads

## Credits

- [José Luis Salinas][link-author]
- [All Contributors][link-contributors]

## License

The MIT License (MIT). Please see [License File](LICENSE.md) for more information.

[link-author]: https://github.com/jotaelesalinas
[link-contributors]: ../../contributors

