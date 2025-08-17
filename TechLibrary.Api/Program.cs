using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TechLibrary.Api.Filters;
using TechLibrary.Api.Infrastructure.DataAccess;

const string AUTHENTICATION_TYPE = "Bearer";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configurar Entity Framework
builder.Services.AddDbContext<TechLibraryDbContext>();

// Configurar CORS para permitir requisições do frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(AUTHENTICATION_TYPE, new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below;
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = AUTHENTICATION_TYPE
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {

            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = AUTHENTICATION_TYPE
                },
                Scheme = "oauth2",
                Name = AUTHENTICATION_TYPE,
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
builder.Services.AddMvc(options => options.Filters.Add(typeof(ExceptionFilter)));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKey()


        };
    });

var app = builder.Build();

// Garantir que o banco de dados e as tabelas existam
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TechLibraryDbContext>();
    db.Database.EnsureCreated();

    // Garantir que a tabela Reservations exista no SQLite do banco legado
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""Reservations"" (
                ""Id"" TEXT NOT NULL UNIQUE,
                ""ReservationDate"" TEXT NOT NULL,
                ""UserId"" TEXT NOT NULL,
                ""BookId"" TEXT NOT NULL,
                ""ExpectedReturnDate"" TEXT NOT NULL,
                ""IsActive"" INTEGER NOT NULL,
                ""CancelledDate"" TEXT,
                ""FulfilledDate"" TEXT,
                FOREIGN KEY(""BookId"") REFERENCES ""Books""(""Id""),
                FOREIGN KEY(""UserId"") REFERENCES ""Users""(""Id""),
                PRIMARY KEY(""Id"")
            );
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Falha ao garantir tabela Reservations: {ex}");
    }

    // Garantir que colunas novas existam em bancos antigos (sem migrações),
    // verificando via PRAGMA para evitar erros de duplicidade
    try
    {
        var connection = db.Database.GetDbConnection();
        connection.Open();

        bool ColumnExists(string table, string column)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info(\"{table}\");";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        string GetSqliteVersion()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "select sqlite_version();";
            var result = cmd.ExecuteScalar()?.ToString() ?? "";
            return result;
        }

        bool IsSqliteDropColumnSupported()
        {
            // DROP COLUMN suportado a partir do SQLite 3.35.0
            var versionText = GetSqliteVersion();
            if (Version.TryParse(versionText, out var version))
            {
                var min = new Version(3, 35, 0);
                return version >= min;
            }
            return false;
        }

        bool IsSqliteRenameColumnSupported()
        {
            // RENAME COLUMN suportado a partir do SQLite 3.25.0
            var versionText = GetSqliteVersion();
            if (Version.TryParse(versionText, out var version))
            {
                var min = new Version(3, 25, 0);
                return version >= min;
            }
            return false;
        }

        // Ajustes na tabela Checkouts
        if (!ColumnExists("Checkouts", "ExpectedReturnDate"))
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Checkouts"" 
                ADD COLUMN ""ExpectedReturnDate"" TEXT NOT NULL DEFAULT (datetime('now','+14 days'));
            ");
        }

        if (!ColumnExists("Checkouts", "ReturnedDate"))
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Checkouts"" 
                ADD COLUMN ""ReturnedDate"" TEXT NULL;
            ");
        }

        // Ajuste da coluna ExpectedReturnDate em Reservations (antiga ExpirationDate)
        if (!ColumnExists("Reservations", "ExpectedReturnDate"))
        {
            var hasOldExpiration = ColumnExists("Reservations", "ExpirationDate");
            if (hasOldExpiration)
            {
                if (IsSqliteRenameColumnSupported())
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Reservations"" RENAME COLUMN ""ExpirationDate"" TO ""ExpectedReturnDate"";");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Startup] Falha ao renomear coluna ExpirationDate -> ExpectedReturnDate: {ex}. Tentando estratégia alternativa.");
                        // Estratégia alternativa: adicionar e copiar
                        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Reservations"" ADD COLUMN ""ExpectedReturnDate"" TEXT NOT NULL DEFAULT (datetime('now')); ");
                        db.Database.ExecuteSqlRaw(@"UPDATE ""Reservations"" SET ""ExpectedReturnDate"" = ""ExpirationDate"" WHERE ""ExpirationDate"" IS NOT NULL;");
                    }
                }
                else
                {
                    // Adicionar nova coluna e copiar dados da antiga
                    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Reservations"" ADD COLUMN ""ExpectedReturnDate"" TEXT NOT NULL DEFAULT (datetime('now')); ");
                    db.Database.ExecuteSqlRaw(@"UPDATE ""Reservations"" SET ""ExpectedReturnDate"" = ""ExpirationDate"" WHERE ""ExpirationDate"" IS NOT NULL;");
                }
            }
            else
            {
                // Não existe nenhuma das duas -> adicionar diretamente
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Reservations"" ADD COLUMN ""ExpectedReturnDate"" TEXT NOT NULL DEFAULT (datetime('now')); ");
            }
        }

        // Se por algum motivo existir a coluna equivocada BookId1 nas Reservations, tentar removê-la
        if (ColumnExists("Reservations", "BookId1"))
        {
            if (IsSqliteDropColumnSupported())
            {
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Reservations"" DROP COLUMN ""BookId1"";");
            }
            else
            {
                Console.WriteLine("[Startup] Encontrada coluna Reservations.BookId1, mas a versão do SQLite não suporta DROP COLUMN (< 3.35). Ignorando.");
            }
        }

        connection.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Falha ao ajustar colunas do banco legado: {ex}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

SymmetricSecurityKey SecurityKey()
{
    var signingKey = "N?wU4bM0rX1K%l<[1?OW{>+dMx{qs7ul";
    return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
}