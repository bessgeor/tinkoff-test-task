using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TinkoffTestTask.Sequences;
using System;

namespace TinkoffTestTask
{
	public class Startup
	{
		public void ConfigureServices( IServiceCollection services )
		{
			services.AddCors();
			services.AddMvc();
			MongoUrl url = new MongoUrl( Environment.GetEnvironmentVariable( "MONGODB_URI" ) );
			IMongoClient client = new MongoClient( url );
			IMongoDatabase database = client.GetDatabase( "heroku_kq19rxxj" );
			services.AddSingleton( database );

			Sequence links = new Sequence { Id = "linksSequence" };
			links.EnsureCreated( database ).GetAwaiter().GetResult();
			services.AddSingleton( links );
		}

		public void Configure( IApplicationBuilder app, IHostingEnvironment env )
		{
			if ( env.IsDevelopment() )
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseDefaultFiles();
			app.UseStaticFiles();

			app.UseCors( builder =>
				 builder.AllowAnyOrigin()
					 .AllowAnyMethod()
					 .AllowAnyHeader()
					 .AllowCredentials()
			);
			app.UseMvcWithDefaultRoute();
		}
	}
}