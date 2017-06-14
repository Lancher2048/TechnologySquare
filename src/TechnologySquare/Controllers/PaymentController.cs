using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechnologySquare.Models;
using TechnologySquare.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;



// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TechnologySquare.Controllers
{
    public class PaymentController : Controller
    {
        private IHostingEnvironment host = null;

        private readonly TechnologySquareContext db;
        public PaymentController(TechnologySquareContext _db, IHostingEnvironment _host)
        {
            db = _db;
            host = _host;
        }



        //// GET: /Payment/

        public ActionResult Index()
        {
            string merId, amt, merTransId, transId, transTime;
            int paymentTypeObjId = int.Parse(Request.Form["paymentTypeObjId"]);
            PaymentType paymentMethod = db.PaymentType.Single(m => m.ObjId == paymentTypeObjId);


            if (RemotePost.PaymentVerify(Request, host.WebRootPath, out merId, out amt, out merTransId, out transId, out transTime) && merId == "Team03")
            {
                Payment pay = db.Payment.Single(m => m.ObjId == int.Parse(merTransId));
                Orders[] orders = db.Orders.Where(m => m.OrderState == 0).ToArray<Orders>();/*ThePayment == int.Parse(merTransId)).ToArray<Orders>();*/
                pay.TransTime = DateTime.Parse(transTime);
                pay.TransNo = transId;
                foreach (Orders or in orders)
                {
                    or.OrderState = 1;
                }
                db.SaveChanges();
                ViewBag.paymentMsg = "付款成功！     付款号：" + merTransId.ToString() + "；   金额：" + amt.ToString() + "元。";//付款成功！显示付款信息作为测试。
            }

            return View();
        }
    }
}
