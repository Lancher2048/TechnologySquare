using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnologySquare.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Http;
using TechnologySquare.Infrastructure;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TechnologySquare.Controllers
{
    public class OrderController : Controller
    {
        private IHostingEnvironment host = null;

        private readonly TechnologySquareContext db;
        public OrderController(TechnologySquareContext _db, IHostingEnvironment _host)
        {
            db = _db;
            host = _host;
        }

        // GET: /<controller>/
        [Authorize]
        public ActionResult Index()
        {
            ViewBag.Request = Request;
            string uid = User.Identity.Name;
            OrderViewModel ovm = new OrderViewModel();
            ovm.orders = new List<OrderInfo>();
            ovm.payment = new Payment();

            //获取信息显示
            ovm.curCustomer = db.Customer.Single(m => m.UserName == uid);
            ViewBag.payments = db.PaymentType.Where(m => m.ObjId > 0).ToArray<PaymentType>();
            List<int[]> curCart = HttpContext.Session.GetJson<List<int[]>>("Cart");
            ovm.orderQty = 0;
            ovm.payment.Amount = 0.0;
            foreach (var cartItem in curCart)
            {
                ovm.orderQty += cartItem[1];
                int pObjId = cartItem[0];
                for (int i = 0; i < cartItem[1]; i++)
                {
                    var product = db.Product.Single(m => m.ObjId == pObjId);
                    ovm.orders.Add(new OrderInfo { theProduct = product.ObjId, price = (double)product.Price, productName = product.Productname });
                    ovm.payment.Amount += product.Price;
                }
            }
            return View("Order", ovm);
        }

        [HttpPost]
        public ActionResult Index(OrderViewModel ovm)
        {
            ViewBag.Request = Request;
            //更新客户联系信息
            Customer curCust = db.Customer.Single(m => m.ObjId == ovm.curCustomer.ObjId);

            if (curCust.MobilePhone != ovm.curCustomer.MobilePhone && ovm.curCustomer.MobilePhone != "")
                curCust.MobilePhone = ovm.curCustomer.MobilePhone;
            if (curCust.Adress != ovm.curCustomer.Adress && ovm.curCustomer.Adress != "")
                curCust.Adress = ovm.curCustomer.Adress;

            db.SaveChanges();
            //保存订单。需做事务处理！在.NET EF core中，一个SaveChange方法所提交的内容会自动实现事务处理。
            bool succeed = true;
            int payId = 0;
            try
            {
                EntityEntry<Payment> p = db.Payment.Add(new Payment());
                p.Entity.Amount = double.Parse(Request.Form["paymentAmt"]);
                p.Entity.ThePaymentType = int.Parse(Request.Form["paymentType"]);
                p.Entity.PaymentState = 0;

                for (int i = 0; i < ovm.orderQty; i++)
                {

                    EntityEntry<Orders> o = db.Orders.Add(new Orders());
                    o.Entity.Customermessage = curCust.ObjId;
                    o.Entity.ThePayment = p.Entity.ThePaymentType;
                    o.Entity.TheProduct = int.Parse(Request.Form["productId_" + i].ToString().Trim());
                    o.Entity.OrderState = 0;
                    o.Entity.OrderTime = DateTime.Now;


                    db.SaveChanges();
                    payId = p.Entity.ObjId;

                }

            }
            catch (Exception e)

            {
                succeed = false;
                Response.WriteAsync(e.ToString());
                //throw (e);
            }
            if (succeed)
            {
                string paymentUrl = "", paymentMethod = "";
                foreach (PaymentType pt in db.PaymentType.Where(m => m.ObjId > 0).ToArray<PaymentType>())
                {
                    if (pt.ObjId == int.Parse(Request.Form["paymentType"]))
                    {
                        paymentUrl = pt.Url;
                        paymentMethod = pt.TypeName;
                        break;
                    }
                }

                PayRequestInfo pri = new PayRequestInfo();
                pri.Amt = Request.Form["paymentAmt"];
                pri.MerId = "Team03";
                pri.MerTransId = payId.ToString();
                pri.PaymentTypeObjId = Request.Form["paymentType"];
                pri.PostUrl = paymentUrl;
                pri.ReturnUrl = "http://" + Request.Host + Url.Action("Index", "Payment");
                pri.CheckValue = RemotePost.getCheckValue(host.WebRootPath, pri.MerId, pri.ReturnUrl, pri.PaymentTypeObjId, pri.Amt, pri.MerTransId);
                return View("PayRequest", pri);
            }
            else
            {
                //如果未能成功保存数据则执行以下行。由于ovm中未能将原来的order等数据带回，这里要重新获取
                ovm.orders = new List<OrderInfo>();
                ovm.payment = new Payment();
                ViewBag.payments = db.PaymentType.Where(m => m.ObjId > 0).ToArray<PaymentType>();
                List<int[]> curCart = HttpContext.Session.GetJson<List<int[]>>("Cart");
                ovm.orderQty = 0;
                ovm.payment.Amount = 0.0;
                foreach (var cartItem in curCart)
                {
                    ovm.orderQty += cartItem[1];
                    int pObjId = cartItem[0];
                    for (int i = 0; i < cartItem[1]; i++)
                    {
                        var product = db.Product.Single(m => m.ObjId == pObjId);
                        ovm.orders.Add(new OrderInfo { theProduct = product.ObjId, price = (double)product.Price });
                        ovm.payment.Amount += product.Price;
                    }
                }
                return View("Order", ovm);
            }

        }

    }
}
