﻿using Microsoft.AspNetCore.Mvc;
using MVW_Proyecto_Mesas_Comida.Models;
using MVW_Proyecto_Mesas_Comida.Services;
using Newtonsoft.Json;

namespace MVW_Proyecto_Mesas_Comida.Controllers
{
	public class UsuariosController : Controller
	{
		private readonly IUsuarioService _usuarioService;
		private readonly IAuthService _authService;

		public UsuariosController(IUsuarioService usuarioService, IAuthService authService)
		{
			_usuarioService = usuarioService;
			_authService = authService;
		}


		[HttpPost]
		public async Task<IActionResult> Register(string nombre, string correo, string contrasena, string confirmarContrasena)
		{
			var captchaResponse = Request.Form["g-recaptcha-response"];
			// Verificar contraseñas
			if (contrasena != confirmarContrasena)
			{
				return Json(new { success = false, message = "Las contraseñas no coinciden." });
			}

			// Verificar si el correo ya existe
			if (await _usuarioService.EmailExists(correo))
			{
				return Json(new { success = false, message = "Este correo ya está registrado." });
			}

			// Validar el token de reCAPTCHA
			var reCaptchaResult = await ValidarReCaptcha(captchaResponse);
			if (!reCaptchaResult)
			{
				return Json(new { success = false, message = "La verificación de reCAPTCHA falló. Inténtalo de nuevo." });
			}

			// Crear el usuario
			var resultado = await _usuarioService.CreateUser(new Usuario { nombre = nombre, correo = correo, contrasena = contrasena });

			if (resultado)
			{
				var usuario = await _usuarioService.GetUsuarioByEmail(correo);
				// Si el registro fue exitoso
				HttpContext.Session.SetString("NombreUsuario", usuario.nombre);
				return Json(new { success = true, message = "Usuario registrado exitosamente." });
			}

			// Si hubo un error al crear el usuario
			return Json(new { success = false, message = "Error al registrar el usuario." });
		}

		
		[HttpPost]
		public async Task<IActionResult> Login(Usuario model)
		{
			// Valida el token de reCAPTCHA
			var captchaResponse = Request.Form["g-recaptcha-response"];
			var reCaptchaResult = await ValidarReCaptcha(captchaResponse);
			if (!reCaptchaResult)
			{
				return Json(new { success = false, message = "La verificación de reCAPTCHA falló. Inténtalo de nuevo." });
			}

			// Intenta iniciar sesión
			var resultado = await _authService.Login(model);
			if (resultado)
			{
				// Obtener el usuario por correo
				var usuario = await _usuarioService.GetUsuarioByEmail(model.correo);

				// Si se encontró el usuario, devolver su nombre
				if (usuario != null)
				{
					HttpContext.Session.SetString("NombreUsuario", usuario.nombre);
					// Verifica el rol del usuario
					if (usuario.rol_id == 1) // 1 para Administrador
					{
						return Json(new { success = true, message = "Inicio de sesión exitoso.", redirectUrl = Url.Action("Index", "Dashboard") });
					}
					else if (usuario.rol_id == 2) // 2 para Comprador
					{
						return Json(new { success = true, message = "Inicio de sesión exitoso.", redirectUrl = Url.Action("index", "Home") });
					}
			
				}
			}

			// Si la autenticación falla, devolver un mensaje de error.
			return Json(new { success = false, message = "Correo o contraseña incorrectos." });
		}



		private async Task<bool> ValidarReCaptcha(string g_recaptcha_response)
		{
			var secretKey = "6LeS8V4qAAAAAGr6jWkABhsLAB6kZcP5kZUE-ptx"; // Coloca tu secret key aquí
			var client = new HttpClient();
			var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={g_recaptcha_response}", null);
			var jsonResponse = await response.Content.ReadAsStringAsync();
			dynamic result = JsonConvert.DeserializeObject(jsonResponse);
			return result.success == "true";
		}


	}
}