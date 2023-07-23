### (byte[], ulong)

| Method |           Job |       Runtime |                UTF16 |      Mean |    Error |   StdDev |   Gen0 | Allocated |
|------- |-------------- |-------------- |--------------------- |----------:|---------:|---------:|-------:|----------:|
|  Parse |    DefaultJob |      .NET 8.0 | :blub(...) list [66] |  60.82 ns | 0.684 ns | 0.606 ns | 0.0267 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :blub(...) list [66] |  50.07 ns | 0.538 ns | 0.504 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  :jtv(...)ers. [100] |  71.52 ns | 0.792 ns | 0.741 ns | 0.0267 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  :jtv(...)ers. [100] |  57.02 ns | 0.996 ns | 0.932 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 | :tmi.(...)ailed [52] |  61.53 ns | 0.948 ns | 0.887 ns | 0.0267 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :tmi.(...)ailed [52] |  52.01 ns | 0.472 ns | 0.442 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)senE [884] | 451.71 ns | 8.833 ns | 9.818 ns | 0.1173 |     736 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)senE [884] | 448.70 ns | 3.588 ns | 3.357 ns | 0.1173 |     736 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)Guys [382] | 387.98 ns | 5.891 ns | 5.511 ns | 0.1121 |     704 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)Guys [382] | 378.16 ns | 1.233 ns | 1.153 ns | 0.1121 |     704 B |
|  Parse |    DefaultJob |      .NET 8.0 | @msg-(...)live. [74] | 115.67 ns | 0.950 ns | 0.842 ns | 0.0355 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | @msg-(...)live. [74] | 117.38 ns | 0.372 ns | 0.290 ns | 0.0356 |     224 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @msg(...)ces. [110] | 120.42 ns | 2.404 ns | 2.361 ns | 0.0355 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @msg(...)ces. [110] | 117.58 ns | 0.818 ns | 0.726 ns | 0.0356 |     224 B |

### (byte[], InnerOffsets)
| Method |           Job |       Runtime |                UTF16 |      Mean |    Error |   StdDev |   Gen0 | Allocated |
|------- |-------------- |-------------- |--------------------- |----------:|---------:|---------:|-------:|----------:|
|  Parse |    DefaultJob |      .NET 8.0 | :blub(...) list [66] |  48.36 ns | 0.349 ns | 0.327 ns | 0.0268 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :blub(...) list [66] |  45.66 ns | 0.375 ns | 0.350 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  :jtv(...)ers. [100] |  46.51 ns | 0.297 ns | 0.278 ns | 0.0268 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  :jtv(...)ers. [100] |  44.82 ns | 0.085 ns | 0.080 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 | :tmi.(...)ailed [52] |  48.50 ns | 0.397 ns | 0.372 ns | 0.0268 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :tmi.(...)ailed [52] |  46.13 ns | 0.571 ns | 0.534 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)senE [884] | 392.10 ns | 1.711 ns | 1.600 ns | 0.1173 |     736 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)senE [884] | 400.26 ns | 0.736 ns | 0.688 ns | 0.1173 |     736 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)Guys [382] | 338.93 ns | 0.479 ns | 0.425 ns | 0.1121 |     704 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)Guys [382] | 348.57 ns | 1.999 ns | 1.772 ns | 0.1121 |     704 B |
|  Parse |    DefaultJob |      .NET 8.0 | @msg-(...)live. [74] |  85.85 ns | 0.575 ns | 0.480 ns | 0.0356 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | @msg-(...)live. [74] |  88.32 ns | 0.603 ns | 0.534 ns | 0.0356 |     224 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @msg(...)ces. [110] |  86.93 ns | 0.157 ns | 0.139 ns | 0.0356 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @msg(...)ces. [110] |  87.52 ns | 0.902 ns | 0.844 ns | 0.0356 |     224 B |

### (byte[], ulong) + bitcast
| Method |           Job |       Runtime |                UTF16 |      Mean |    Error |   StdDev |   Gen0 | Allocated |
|------- |-------------- |-------------- |--------------------- |----------:|---------:|---------:|-------:|----------:|
|  Parse |    DefaultJob |      .NET 8.0 | :blub(...) list [66] |  70.38 ns | 0.767 ns | 0.717 ns | 0.0267 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :blub(...) list [66] |  51.11 ns | 0.625 ns | 0.584 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  :jtv(...)ers. [100] |  78.10 ns | 1.102 ns | 1.031 ns | 0.0267 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  :jtv(...)ers. [100] |  58.80 ns | 0.418 ns | 0.391 ns | 0.0267 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 | :tmi.(...)ailed [52] |  68.15 ns | 0.181 ns | 0.169 ns | 0.0267 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :tmi.(...)ailed [52] |  52.55 ns | 0.155 ns | 0.137 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)senE [884] | 445.69 ns | 1.116 ns | 1.044 ns | 0.1173 |     736 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)senE [884] | 438.54 ns | 1.439 ns | 1.346 ns | 0.1173 |     736 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)Guys [382] | 382.62 ns | 1.697 ns | 1.587 ns | 0.1121 |     704 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)Guys [382] | 387.74 ns | 2.224 ns | 2.080 ns | 0.1121 |     704 B |
|  Parse |    DefaultJob |      .NET 8.0 | @msg-(...)live. [74] | 126.01 ns | 0.230 ns | 0.215 ns | 0.0355 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | @msg-(...)live. [74] | 101.52 ns | 0.293 ns | 0.260 ns | 0.0356 |     224 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @msg(...)ces. [110] | 130.74 ns | 0.981 ns | 0.918 ns | 0.0355 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @msg(...)ces. [110] | 101.51 ns | 0.300 ns | 0.280 ns | 0.0356 |     224 B |

### flattened
| Method |           Job |       Runtime |                UTF16 |      Mean |    Error |   StdDev |   Gen0 | Allocated |
|------- |-------------- |-------------- |--------------------- |----------:|---------:|---------:|-------:|----------:|
|  Parse |    DefaultJob |      .NET 8.0 | :blub(...) list [66] |  47.10 ns | 0.430 ns | 0.336 ns | 0.0268 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :blub(...) list [66] |  45.72 ns | 0.372 ns | 0.330 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  :jtv(...)ers. [100] |  44.44 ns | 0.682 ns | 0.638 ns | 0.0268 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  :jtv(...)ers. [100] |  45.35 ns | 0.065 ns | 0.054 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 | :tmi.(...)ailed [52] |  47.58 ns | 0.069 ns | 0.064 ns | 0.0268 |     168 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | :tmi.(...)ailed [52] |  46.65 ns | 0.458 ns | 0.383 ns | 0.0268 |     168 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)senE [884] | 370.67 ns | 1.365 ns | 1.139 ns | 0.1173 |     736 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)senE [884] | 402.65 ns | 0.729 ns | 0.569 ns | 0.1173 |     736 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @bad(...)Guys [382] | 342.15 ns | 0.766 ns | 0.598 ns | 0.1121 |     704 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @bad(...)Guys [382] | 352.95 ns | 2.757 ns | 2.579 ns | 0.1121 |     704 B |
|  Parse |    DefaultJob |      .NET 8.0 | @msg-(...)live. [74] |  86.67 ns | 0.749 ns | 0.700 ns | 0.0356 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 | @msg-(...)live. [74] |  91.07 ns | 1.344 ns | 1.191 ns | 0.0356 |     224 B |
|  Parse |    DefaultJob |      .NET 8.0 |  @msg(...)ces. [110] |  88.76 ns | 0.765 ns | 0.679 ns | 0.0356 |     224 B |
|  Parse | NativeAOT 8.0 | NativeAOT 8.0 |  @msg(...)ces. [110] |  88.21 ns | 0.998 ns | 0.933 ns | 0.0356 |     224 B |