// vue.config.js
module.exports = {
  indexPath: 'player.html',
  publicPath: process.env.NODE_ENV === 'production' ? '/prose/' : '/',
  //chainWebpack: config => {
  //  config.module.rules.delete('eslint');
  //},
}
