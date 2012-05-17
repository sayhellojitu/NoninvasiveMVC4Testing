using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal.Fakes;
using System.Web.Fakes;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.Security.Fakes;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoninvasiveMVC4Testing.Controllers;
using NoninvasiveMVC4Testing.Models;
using Assert = NUnit.Framework.Assert;

namespace NoninvasiveMVC4Testing.Tests
{
    [TestClass]
    public class AccountsControllerTests
    {
        [TestMethod]
        public void TestLogOff()
        {
            var accountController = new AccountController();
            var formsAuthenticationSignOutCalled = false;
            RedirectToRouteResult redirectToRouteResult;

            //Scope the detours we're creating
            using (ShimsContext.Create())
            {
                //Detours FormsAuthentication.SignOut() to an empty implementation
                ShimFormsAuthentication.SignOut = () =>
                {
                    //Set a boolean to identify that we actually got here
                    formsAuthenticationSignOutCalled = true;
                };
                redirectToRouteResult = accountController.LogOff() as RedirectToRouteResult;
                Assert.AreEqual(true, formsAuthenticationSignOutCalled);
            }

            Assert.NotNull(redirectToRouteResult);
            Assert.AreEqual("Index", redirectToRouteResult.RouteValues["Action"]);
            Assert.AreEqual("Home", redirectToRouteResult.RouteValues["controller"]);
        }

        [TestMethod]
        public void TestJsonLogin()
        {
            string testUserName = "TestUserName";
            string testPassword = "TestPassword";
            bool testRememberMe = false;
            string testReturnUrl = "TestReturnUrl";

            var loginModel = new LoginModel
            {
                UserName = testUserName,
                Password = testPassword,
                RememberMe = testRememberMe
            };

            var accountController = new AccountController();
            JsonResult jsonResult;
            //Scope the detours we're creating
            using (ShimsContext.Create())
            {
                //Sets up a detour for Membership.ValidateUser to our mocked implementation
                ShimMembership.ValidateUserStringString = (userName, password) =>
                {
                    Assert.AreEqual(testUserName, userName);
                    Assert.AreEqual(testPassword, password);
                    return true;
                };

                //Sets up a detour for FormsAuthentication.SetAuthCookie to our mocked implementation
                ShimFormsAuthentication.SetAuthCookieStringBoolean = (userName, rememberMe) =>
                {
                    Assert.AreEqual(testUserName, userName);
                    Assert.AreEqual(testRememberMe, rememberMe);
                };

                jsonResult = accountController.JsonLogin(loginModel, testReturnUrl);
            }

            var success = (bool)(new PrivateObject(jsonResult.Data, "success")).Target;
            var redirect = (string)(new PrivateObject(jsonResult.Data, "redirect")).Target;

            Assert.AreEqual(true, success);
            Assert.AreEqual(testReturnUrl, redirect);
        }

        [TestMethod]
        public void TestInvalidJsonLogin()
        {
            string testUserName = "TestUserName";
            string testPassword = "TestPassword";
            bool testRememberMe = false;
            string testReturnUrl = "TestReturnUrl";

            var loginModel = new LoginModel
            {
                UserName = testUserName,
                Password = testPassword,
                RememberMe = testRememberMe
            };

            var accountController = new AccountController();
            JsonResult jsonResult;
            //Scope the detours we're creating
            using (ShimsContext.Create())
            {
                //Sets up a detour for Membership.ValidateUser to our mocked implementation
                ShimMembership.ValidateUserStringString = (userName, password) => false;
                jsonResult = accountController.JsonLogin(loginModel, testReturnUrl);
            }

            var errors = (IEnumerable<string>)(new PrivateObject(jsonResult.Data, "errors")).Target;
            Assert.AreEqual("The user name or password provided is incorrect.", errors.First());
        }

        [TestMethod]
        public void TestLogin()
        {
            string testUserName = "TestUserName";
            string testPassword = "TestPassword";
            bool testRememberMe = false;
            string returnUrl = "/foo.html";

            var loginModel = new LoginModel
            {
                UserName = testUserName,
                Password = testPassword,
                RememberMe = testRememberMe
            };

            var accountController = new AccountController();

            //Setup underpinning via stubbing such that UrlHelper 
            //can validate that our "foo.html" is local
            var stubHttpContext = new StubHttpContextBase();
            var stubHttpRequestBase = new StubHttpRequestBase();
            stubHttpContext.RequestGet = () => stubHttpRequestBase;
            var requestContext = new RequestContext(stubHttpContext, new RouteData());
            accountController.Url = new UrlHelper(requestContext);

            RedirectResult redirectResult;
            //Scope the detours we're creating
            using (ShimsContext.Create())
            {
                //Sets up a detour for Membership.ValidateUser to our mocked implementation
                ShimMembership.ValidateUserStringString = (userName, password) =>
                {
                    Assert.AreEqual(testUserName, userName);
                    Assert.AreEqual(testPassword, password);
                    return true;
                };

                //Sets up a detour for FormsAuthentication.SetAuthCookie to our mocked implementation
                ShimFormsAuthentication.SetAuthCookieStringBoolean = (userName, rememberMe) =>
                {
                    Assert.AreEqual(testUserName, userName);
                    Assert.AreEqual(testRememberMe, rememberMe);
                };

                redirectResult = accountController.Login(loginModel, returnUrl) as RedirectResult;
            }

            Assert.NotNull(redirectResult);
            Assert.AreEqual(redirectResult.Url, returnUrl);
        }

        [TestMethod]
        public void TestJsonRegister()
        {
            string testUserName = "TestUserName";
            string testPassword = "TestPassword";
            string testConfirmPassword = "TestPassword";
            string testEmail = "TestEmail@Test.com";

            var registerModel = new RegisterModel
            {
                UserName = testUserName,
                Password = testPassword,
                ConfirmPassword = testConfirmPassword,
                Email = testEmail
            };

            var accountController = new AccountController();
            JsonResult jsonResult;
            //Scope the detours we're creating
            using (ShimsContext.Create())
            {
                //Sets up a detour for Membership.CreateUser to our mocked implementation
                ShimMembership.CreateUserStringStringStringStringStringBooleanObjectMembershipCreateStatusOut =
                    (string userName, string password, string email, string passwordQuestion, 
                        string passwordAnswer, bool isApproved, object providerUserKey,
                        out MembershipCreateStatus @createStatus) =>
                    {
                        Assert.AreEqual(testUserName, userName);
                        Assert.AreEqual(testPassword, password);
                        Assert.AreEqual(testEmail, email);
                        Assert.Null(passwordQuestion);
                        Assert.Null(passwordAnswer);
                        Assert.True(isApproved);
                        Assert.Null(providerUserKey);
                        @createStatus = MembershipCreateStatus.Success;

                        return null;
                    };

                //Sets up a detour for FormsAuthentication.SetAuthCookie to our mocked implementation
                ShimFormsAuthentication.SetAuthCookieStringBoolean = (userName, rememberMe) =>
                {
                    Assert.AreEqual(testUserName, userName);
                    Assert.AreEqual(false, rememberMe);
                };

                var actionResult = accountController.JsonRegister(registerModel);
                Assert.IsInstanceOf(typeof(JsonResult), actionResult);
                jsonResult = actionResult as JsonResult;
            }

            Assert.NotNull(jsonResult);
            var success = (bool)(new PrivateObject(jsonResult.Data, "success")).Target;
            Assert.True(success);
        }

        [TestMethod]
        public void TestChangePassword()
        {
            string testUserName = "TestUserName";
            string testOldPassword = "TestOldPassword";
            string testNewPassword = "TestNewPassword";

            var changePasswordModel = new ChangePasswordModel
            {
                OldPassword = testOldPassword,
                NewPassword = testNewPassword
            };

            var accountController = new AccountController();

            //Stub HttpContext
            var stubHttpContext = new StubHttpContextBase();
            //Setup ControllerContext so AccountController will use our stubHttpContext
            accountController.ControllerContext = new ControllerContext(stubHttpContext, 
                new RouteData(), accountController);

            //Stub IPrincipal
            var principal = new StubIPrincipal();
            principal.IdentityGet = () =>
            {
                var identity = new StubIIdentity { NameGet = () => testUserName };
                return identity;
            };
            stubHttpContext.UserGet = () => principal;

            RedirectToRouteResult redirectToRouteResult;
            //Scope the detours we're creating
            using (ShimsContext.Create())
            {
                ShimMembership.GetUserStringBoolean = (identityName, userIsOnline) =>
                {
                    Assert.AreEqual(testUserName, identityName);
                    Assert.AreEqual(true, userIsOnline);

                    var memberShipUser = new ShimMembershipUser();
                    //Sets up a detour for MemberShipUser.ChangePassword to our mocked implementation
                    memberShipUser.ChangePasswordStringString = (oldPassword, newPassword) =>
                    {
                        Assert.AreEqual(testOldPassword, oldPassword);
                        Assert.AreEqual(testNewPassword, newPassword);
                        return true;
                    };
                    return memberShipUser;
                };

                var actionResult = accountController.ChangePassword(changePasswordModel);
                Assert.IsInstanceOf(typeof(RedirectToRouteResult), actionResult);
                redirectToRouteResult = actionResult as RedirectToRouteResult;
            }
            Assert.NotNull(redirectToRouteResult);
            Assert.AreEqual("ChangePasswordSuccess", redirectToRouteResult.RouteValues["Action"]);
        }
    }
}
