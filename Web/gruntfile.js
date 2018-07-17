module.exports = function (grunt) {

    grunt.loadNpmTasks('grunt-markdown');
    grunt.loadNpmTasks('grunt-contrib-less');
    grunt.loadNpmTasks('grunt-contrib-cssmin');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-watch');

    grunt.initConfig({
        markdown: {
            all: {
              files: [
                {
                  expand: true,
                  src: '../Docs/*.md',
                  dest: 'Docs/',
                  ext: '.html'
                }
              ],
              options: {
                template: '../Docs/blanktemplate.jst'
              }
            }
        },
        less: {
            all: {
                options: {
                    paths: ['wwwroot/css']
                },
                files: {
                    'wwwroot/css/site.css': 'wwwroot/css/site.less'
                }
            }
        },
        cssmin: {
            target: {
                files: {
                    'wwwroot/css/site.min.css': 'wwwroot/css/site.css'
                }
            }
        },
        copy: {
          libs: {
            files: 
            [
                {
                    expand: true,
                    cwd: 'node_modules',
                    src: ['bootstrap/dist/**'],
                    dest: 'wwwroot/lib/'
                },
                {
                    expand: true,
                    cwd: 'node_modules',
                    src: ['jquery/dist/**'],
                    dest: 'wwwroot/lib/'
                }
            ],
          },
        },
        watch: {
            docs: {
                files: ['../Docs/*.md'],
                tasks: ['markdown:all']
            },
            less: {
                files: ['wwwroot/css/*.less'],
                tasks: ['less:all']
            }
        }
    });
};
