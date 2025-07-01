using MediatR;
using Microsoft.Data.SqlClient;
using Pd_Ws_Unoee;
using Serilog;
using Ts_Ws_Unoee;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddTransient<SqlConnection>((sp) => new SqlConnection(builder.Configuration.GetConnectionString("DBConnection")));
builder.Services.AddCors(options => { options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); });

try
{
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Seq("http://localhost:5341")
        .CreateLogger();
}
catch (Exception ex)
{
    File.AppendAllText("log_startup_fallback.txt", ex.ToString());
}

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<object>(provider =>
        new Pd_WSUNOEESoapClient(Pd_WSUNOEESoapClient.EndpointConfiguration.WSUNOEESoap));
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<object>(provider =>
        new Ts_WSUNOEESoapClient(Ts_WSUNOEESoapClient.EndpointConfiguration.WSUNOEESoap));
}
else
{
    throw new InvalidOperationException("Invalid environment configuration");
}

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<Error_Handling_Middleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapControllers();

app.Run();
