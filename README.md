# Chaining
![.NET Core](https://github.com/sjoerdvanloon/Chaining/workflows/.NET%20Core/badge.svg?branch=master)

Voorbeeldje voor Gijs en Bram om het maken van scenario's beter te laten gaan met waits, maar ook met logging.

Het zou ook mogelijk kunnen zijn om de acties later pas uit te voren

``` C#
// Manier om acties op een pagina uit te voeren en uiteindelijk op een status te wachten
On<DotDotPage>().Do(x => x.FillIn()).And.Do(x => x.Logon()).Until(5, x => x.GetValue());
// Of
On<DotDotPage>()
  .Do(x => x.FillIn())
  .And.Do(x => x.Logon())
  .Until(5, x => x.GetValue());
```

``` C#
// Manier om rits acties op een pagina uit te voeren
On<DotDotPage>().Do(x => x.FillIn(), x => x.FillIn(), x => x.FillIn(), x => x.FillIn());
// Of
On<DotDotPage>().Do(
  x => x.FillIn(), 
  x => x.Logon(), 
  x => x.FillIn(), 
  x => x.Logon()
);
```

Voorbeeld log
``` text
Page DotDotPage
---------------
 - FillIn
   - Ola, fill in
 - Logon
   - Ola, llogon
 - GetValue
   - Getting value 3
   - Getting value 4
   - Getting value 5
 ```
