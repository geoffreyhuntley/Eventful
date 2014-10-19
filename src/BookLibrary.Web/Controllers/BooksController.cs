﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FSharpx;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using FSharpx.Collections;

namespace BookLibrary.Web.Controllers
{
    public class BooksController : Controller
    {
        private readonly BookLibrarySystem _system;

        public BooksController(BookLibrarySystem system)
        {
            _system = system;
        }

        // GET: Books
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create(AddBookCommand cmd)
        {
            cmd.BookId = new BookId(Guid.NewGuid());

            var errors = Book.validateCommand(cmd);
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    var field = OptionModule.IsSome(error.Item1) ? error.Item1.Value : "";
                    var errorMessage = error.Item2;
                    ModelState.AddModelError(field, errorMessage);
                }
                return View();
            }
            else
            {
                var result = await _system.RunCommand(cmd);
                if (result.IsChoice1Of2)
                {
                    return View("Success");
                }
                else
                {
                    //var errorResult = ((FSharpChoice<FSharpList<Tuple<string, object, BookLibraryEventMetadata>>, FSharpx.Collections.NonEmptyList<CommandFailure>>.Choice2Of2)result).Item;
                    //foreach (var error in errorResult)
                    //{
                    //    var commandError = error as CommandFailure.CommandError;
                    //    if (commandError != null)
                    //    {
                    //        ModelState.AddModelError("", commandError.Item);
                    //    }

                    //    var commandExn = error as CommandFailure.CommandException;
                    //    if (commandExn != null)
                    //    {
                    //        ModelState.AddModelError("", commandExn.Item1 + ": " + commandExn.Item2);
                    //    }
                    //    var fieldError = error as CommandFailure.CommandFieldError;
                    //    if (fieldError != null)
                    //    {
                    //        ModelState.AddModelError(fieldError.Item1, fieldError.Item2);
                    //    }
                    //}
                    return View();
                }
            }
        }
    }
}