FROM nginx:alpine

# Remove default configuration and inject our production config
RUN rm -rf /etc/nginx/conf.d/*
COPY nginx.conf /etc/nginx/nginx.conf

# Expose HTTP port 80 and HTTPS port 443
EXPOSE 80 443

# Health check verifies Nginx is serving
HEALTHCHECK --interval=15s --timeout=5s --start-period=5s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
