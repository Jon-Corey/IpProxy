# IP Proxy

This is a proxy meant for use by [Device Info](https://github.com/Jon-Corey/DeviceInfo). It forwards a request to [IP-API](https://ip-api.com/)'s free-tier non-SSL API. This proxy is intended to allow clients to get the data from [IP-API](https://ip-api.com/) without having to make a request to a non-SSL resource (which the browser blocks on HTTPS sites).

## Can I Call This Function From My Own Site?

No. Please do not call this Azure Function from your own app/site. If too many requests hit my Azure Function, I will have to pay for those overages, which will lead to me having to lock this down and make the user experience worse.

You can however take this code and deploy it to your own Azure Function. Azure Functions have a free tier that is very generous. It's also pretty easy to set up.

Depending on your use case, another option may be to call [IP-API](https://ip-api.com/)'s free-tier API directly.
