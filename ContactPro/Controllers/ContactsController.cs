﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactPro.Data;
using ContactPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ContactPro.Enums;
using ContactPro.Models.ViewModels;
using ContactPro.Services.Interfaces;
using ContactPro.Services;
using Microsoft.AspNetCore.Identity.UI.Services;


namespace ContactPro.Controllers
{
    public class ContactsController : Controller
    {
        // These are all being injected into the class and initialised as private varables (standard practice is to start private vars with _
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;
        private readonly IEmailService _emailService;


        public ContactsController(ApplicationDbContext context, 
                                  UserManager<AppUser> userManager,
                                  IImageService imageService,
                                  IAddressBookService addressBookService,
                                  IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;  
            _addressBookService = addressBookService;
            _emailService = emailService;
        }

        // GET: Contacts
        [Authorize]
        public async Task<IActionResult> Index(int categoryId, string swalMessage = null)
        //IActionResult means it returns a view
        {
            ViewData["SwalMessage"] = swalMessage;


            List<Contact> contacts = new List<Contact>();
            //  Something WRONG HERE NOT SURE WHAT YET var contacts = new List<Contact>();
            string appUserId = _userManager.GetUserId(User);

            //return the userID and its associated contacts and categories;
            AppUser? appUser =  _context.Users
                .Include(c => c.Contacts)
                .ThenInclude(c => c.Categories)
                .FirstOrDefault(u => u.Id == appUserId);
            
            // AppUser? appUser = _context.Users
            //     .FirstOrDefault(u => u.Id == appUserId);
            //
            //
            // var categories = await _context.Categories.Where(c => c.AppUserId == appUserId)
            //     .Include(c => c.AppUser)

            var categories = appUser.Categories;

            if(categoryId == 0){  
                contacts = appUser.Contacts.OrderBy(c => c.LastName)
                                        .ThenBy(c => c.FirstName)
                                        .ToList();
            }
            else
            {
                contacts = appUser.Categories.FirstOrDefault(c => c.Id == categoryId)
                                    .Contacts
                                    .OrderBy(c => c.LastName)
                                    .ThenBy(c => c.FirstName)
                                    .ToList();
            }

            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", categoryId);


            return View(contacts);
        }

        [Authorize]
        public  IActionResult SearchContacts(string searchString)
        {
            string appUserId = _userManager.GetUserId(User);
            var contacts = new List<Contact>();
            
            
            AppUser? appUser =  _context.Users
                                    .Include(c => c.Contacts)
                                    .ThenInclude(c => c.Categories)
                                    .FirstOrDefault(u => u.Id == appUserId);
            
            if (String.IsNullOrEmpty(searchString))
            {
                contacts = appUser.Contacts
                                    .OrderBy(c => c.LastName)
                                    .ThenBy(c => c.FirstName)
                                    .ToList();
            }
            else
            {
                contacts = appUser.Contacts.Where(c => c.FullName!.ToLower().Contains(searchString.ToLower()))
                                    .OrderBy(c => c.LastName)
                                    .ThenBy(c => c.FirstName)
                                    .ToList();
            }

            ViewData["CategoryId"] = new SelectList(appUser.Categories, "Id", "Name", 0);   

            return View(nameof(Index), contacts);
        }

        // GET
        [Authorize]
        public async Task<IActionResult> EmailContact(int id)
        {
            string appUserId = _userManager.GetUserId(User);

            Contact contact = await _context.Contacts.Where(c => c.Id == id && c.AppUserId == appUserId)
                .FirstOrDefaultAsync();

            if (contact == null)
            {
                return NotFound();
            }

            EmailData emailData = new EmailData()
            {
                Address = contact.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName
            };

            EmailContactViewModel model = new EmailContactViewModel()
            {
                Contact = contact,
                EmailData = emailData
            };
            
            return View(model);
        }
        
        //POST
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EmailContact(EmailContactViewModel ecvm)
        {
            // If the values from emailData and EmailContactViewModel  match up with the values sent from the EmailContact view
            if (ModelState.IsValid)
            {
                try
                {
                    await _emailService.SendEmailAsync(ecvm.EmailData.Address, ecvm.EmailData.Subject,
                        ecvm.EmailData.Body);
                    // Last parameter is just a plain new object
                    return RedirectToAction("Index", "Contacts", new{ swalMessage = "Success: Email Sent!" });
                }
                catch
                {
                    return RedirectToAction("Index", "Contacts", new{ swalMessage = "Error: Email Send Failed!" });
                }
            }

            return View(ecvm);
        }
        
        // GET: Contacts/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            string appUserId = _userManager.GetUserId(User); 

            ViewData["CountiesList"] = new SelectList(Enum.GetValues(typeof(Counties)).Cast<Counties>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId), "Id","Name");
            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,BirthDate,Address1,Address2,City,County,Email,PostCode,PhoneNumber,ImageFile")] Contact contact, List<int> CategoryList)
        {
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {

                contact.AppUserId = _userManager.GetUserId(User);
                contact.Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);


                if (contact.BirthDate != null)
                {
                    contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                }

                if (contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType; 
                }
                
                _context.Add(contact);
                await _context.SaveChangesAsync();

                //loop over all the selected categories
                foreach (int categoryId in CategoryList)
                {
                    await _addressBookService.AddContactToCategoryAsync(categoryId, contact.Id);
                }
                //save each category selected to the contact categories table.

                return RedirectToAction(nameof(Index));
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Contacts/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            string appUserId = _userManager.GetUserId(User);

            // original scaffolded line: var contact = await _context.Contacts.FindAsync(id);
            // OG line is wrong because there's a security issue where a client could put in a different id and get back contacts that dont belong to them.
            var contact = await _context.Contacts.Where(c => c.Id == id && c.AppUserId == appUserId)
                                                 .FirstOrDefaultAsync();
            
            if (contact == null)
            {
                return NotFound();
            }

            ViewData["CountiesList"] = new SelectList(Enum.GetValues(typeof(Counties)).Cast<Counties>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId), "Id", "Name", await _addressBookService.GetContactCategoryIdsAsync(contact.Id));
            
            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Anything named in the form on the front end that is pushed can be accessed here as a paramenter. Hence CategoryList isn't in the Contacts model but we still have access to it here.
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserId,FirstName,LastName,BirthDate,Address1,Address2,City,County,PostCode,Email,PhoneNumber,Created,ImageFile,ImageData,ImageType")] Contact contact, List<int> CategoryList)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // We need to override the default date object so it fits into the database
                    contact.Created = DateTime.SpecifyKind(contact.Created, DateTimeKind.Utc);

                    if(contact.BirthDate != null)
                    {
                        // We need to override the default date object so it fits into the database
                        contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                    }

                    if (contact.ImageFile != null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;

                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    // Save our categories
                    //first step is to remove current categories
                    List<Category> oldCategories = (await _addressBookService.GetContactCategoriesAsync(contact.Id)).ToList();
                    foreach (var category in oldCategories)
                    {
                        await _addressBookService.RemoveContactFromCategoryAsync(category.Id, contact.Id);
                    }

                    //add the selected categories

                    foreach(int categoryId in CategoryList)
                    {
                        await _addressBookService.AddContactToCategoryAsync(categoryId, contact.Id);
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", contact.AppUserId);
            return View(contact);
        }

        // GET: Contacts/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            string appUserId = _userManager.GetUserId(User);
            
            // In previous examples of the following code we use a where clause but we can also add the filtering logic to FirstOrDefaultAsync
            var contact = await _context.Contacts
                      .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == appUserId);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string appUserId = _userManager.GetUserId(User);

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == appUserId);

            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
          return _context.Contacts.Any(e => e.Id == id);
        }
    }
}
